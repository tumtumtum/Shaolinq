// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Platform;

namespace Shaolinq
{
	public class RelatedDataAccessObjects<T>
		: DataAccessObjectsQueryable<T>, IRelatedDataAccessObjectContext, IDataAccessObjectActivator
		where T : DataAccessObject
	{
		public override Type ElementType { get { return typeof(T); } }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; private set; }

		public string PropertyName { get; private set; }
		public EntityRelationshipType RelationshipType { get; private set; }
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; private set; }

		public RelatedDataAccessObjects(IDataAccessObjectAdvanced relatedDataAccessObject, DataAccessModel dataAccessModel, EntityRelationshipType relationshipType, string propertyName)
			: base(dataAccessModel, null)
		{
			this.PropertyName = propertyName;
			this.RelatedDataAccessObject = relatedDataAccessObject;
			this.RelationshipType = relationshipType;
			this.ExtraCondition = GetExtraCondition();
			this.PersistenceQueryProvider.RelatedDataAccessObjectContext = this;

			BuildInitializeRelatedMethod();
		}

		private LambdaExpression GetExtraCondition()
		{
			switch (this.RelationshipType)
			{
				case EntityRelationshipType.ParentOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T), "param");

					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					var prop = newObjectTypeDescriptor.GetRelatedProperty(this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType()));
					var body = Expression.Equal(Expression.MakeMemberAccess(param, prop.PropertyInfo), Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(body, param);
				}
				case EntityRelationshipType.OneToOne:
				case EntityRelationshipType.ChildOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T), "param");
					var body = Expression.Equal(param, Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(body, param);
				}
				default:
				{
					throw new NotSupportedException(this.RelationshipType.ToString());
				}
			}
		}
		
		private void BuildInitializeRelatedMethod()
		{
			var key = new Pair<Type, Type>(this.RelatedDataAccessObject.GetType(), typeof(T));
			var cache = this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache;

			Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> initializeDataAccessObject;

			if (cache.TryGetValue(key, out initializeDataAccessObject))
			{
				this.InitializeDataAccessObject = initializeDataAccessObject;

				return;
			}

			switch (this.RelationshipType)
			{
				case EntityRelationshipType.ParentOfOneToMany:
				{
					var relatedDataAccessObjectType = this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType());
					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					
					var newParam = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "newobj");
					var relatedParam = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "related");

					var propertyDescriptor = newObjectTypeDescriptor.RelatedProperties.First(c => relatedDataAccessObjectType.IsAssignableFrom(c.PropertyType));

					var method = propertyDescriptor.PropertyInfo.GetSetMethod();

					var body = Expression.Call(Expression.Convert(newParam, typeof(T)), method, Expression.Convert(relatedParam, relatedDataAccessObjectType));

					var lambda = Expression.Lambda(body, relatedParam, newParam);

					this.InitializeDataAccessObject = (Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>)lambda.Compile();

					var newCache = new Dictionary<Pair<Type, Type>, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(cache);

					newCache[key] = this.InitializeDataAccessObject;

					this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache = newCache;

					break;
				}
				case EntityRelationshipType.ChildOfOneToMany:
				{
					break;	
				}
			}
		}

		public override T Create()
		{
			var retval = base.Create();

			this.InitializeDataAccessObject(this.RelatedDataAccessObject, retval);

			return retval;
		}

		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create()
		{
			return this.Create();
		}
	}
}
