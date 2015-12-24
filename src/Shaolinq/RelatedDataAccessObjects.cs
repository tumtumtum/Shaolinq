// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class RelatedDataAccessObjects<T>
		: DataAccessObjectsQueryable<T>, IRelatedDataAccessObjectContext, IDataAccessObjectActivator, IHasExtraCondition
		where T : DataAccessObject
	{
		private List<T> values;
		public EntityRelationshipType RelationshipType { get; }
		public LambdaExpression ExtraCondition { get; protected set; }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create() => this.Create();
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		public override IEnumerator<T> GetEnumerator() => this.values?.GetEnumerator() ?? base.GetEnumerator();

		public RelatedDataAccessObjects(IDataAccessObjectAdvanced relatedDataAccessObject, DataAccessModel dataAccessModel, EntityRelationshipType relationshipType)
			: base(dataAccessModel)
		{
			this.RelatedDataAccessObject = relatedDataAccessObject;
			this.RelationshipType = relationshipType;
			this.ExtraCondition = this.CreateJoinCondition();
			this.PersistenceQueryProvider.RelatedDataAccessObjectContext = this;
			this.InitializeDataAccessObject = this.GetInitializeRelatedMethod();
		}

		public virtual RelatedDataAccessObjects<T> Invalidate()
		{
			this.values = null;

			return this;
		}

		internal void SetValues(List<T> values)
		{
			this.values = values;
		}
		
		private LambdaExpression CreateJoinCondition()
		{
			switch (this.RelationshipType)
			{
				case EntityRelationshipType.ParentOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T));

					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					var prop = newObjectTypeDescriptor.GetRelatedProperty(this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType()));
					var body = Expression.Equal(Expression.MakeMemberAccess(param, prop.PropertyInfo), Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(body, param);
				}
				case EntityRelationshipType.OneToOne:
				case EntityRelationshipType.ChildOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T));
					var body = Expression.Equal(param, Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(body, param);
				}
				default:
				{
					throw new NotSupportedException(this.RelationshipType.ToString());
				}
			}
		}
		
		private Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> GetInitializeRelatedMethod()
		{
			var key = new Tuple<Type, Type>(this.RelatedDataAccessObject.GetType(), typeof(T));
			var cache = this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache;

			Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> initializeDataAccessObject;

			if (cache.TryGetValue(key, out initializeDataAccessObject))
			{
				return initializeDataAccessObject;
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
					var retval = (Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>)lambda.Compile();

					var newCache = new Dictionary<Tuple<Type, Type>, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(cache)
					{
						[key] = retval
					};

					this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache = newCache;

					return retval;
				}
				case EntityRelationshipType.ChildOfOneToMany:
				{
					break;
				}
			}

			return null;
		}

		public override T Create()
		{
			var retval = base.Create();

			this.InitializeDataAccessObject?.Invoke(this.RelatedDataAccessObject, retval);

			return retval;
		}
	}
}
