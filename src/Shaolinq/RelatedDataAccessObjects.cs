// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;
using Platform;

namespace Shaolinq
{
	public class RelatedDataAccessObjects<T>
		: DataAccessObjectsQueryable<T>, IRelatedDataAccessObjectContext, IDataAccessObjectActivator
		where T : class, IDataAccessObject
	{
		public IDataAccessObject RelatedDataAccessObject { get; private set; }

		public override Type ElementType
		{
			get
			{
				return typeof(T);
			}
		}
		
		public string PropertyName { get; private set; }
		public EntityRelationshipType RelationshipType { get; private set; }
		public Action<IDataAccessObject, IDataAccessObject> InitializeDataAccessObject { get; private set; }
		
		public virtual void Initialize(IDataAccessObject relatedDataAccessObject, DataAccessModel dataAccessModel, EntityRelationshipType relationshipType, string propertyName)
		{
			base.Initialize(dataAccessModel, null);

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
					
					Expression cond = null;

					var prop = newObjectTypeDescriptor.GetRelatedProperty(this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType()));

					cond = Expression.Equal(Expression.MakeMemberAccess(param, prop.PropertyInfo), Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(cond, param);
				}
				case EntityRelationshipType.ChildOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T), "param");

					var relatedDataAccessObjectType = this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType());
					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					var relatedTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(relatedDataAccessObjectType);
					
					var leftExpressions = new List<Expression>();
					var rightExpressions = new List<Expression>();
					
					var prop = relatedTypeDescriptor.GetRelatedProperty(this.DataAccessModel.GetDefinitionTypeFromConcreteType(typeof(T)));
					var concreteType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(relatedDataAccessObjectType);

					var leftPropertyNames = new List<string>();
					var rightPropertyNames = new List<string>();
					
					foreach (var primaryKey in newObjectTypeDescriptor.PrimaryKeyProperties)
					{
						var keyProp = concreteType.GetProperties().FirstOrDefault(c => c.Name == prop.PropertyName + primaryKey.PersistedShortName);

						rightExpressions.Add(Expression.Constant(keyProp.GetValue(this.RelatedDataAccessObject, null)));
						leftExpressions.Add(Expression.Property(param, primaryKey.PropertyInfo));

						leftPropertyNames.Add(primaryKey.PropertyName);
						rightPropertyNames.Add(keyProp.Name);
					}

					var body = Expression.Equal(new SqlObjectOperand(typeof(T), leftExpressions, leftPropertyNames), new SqlObjectOperand(typeof(T), rightExpressions, rightPropertyNames));

					return Expression.Lambda(body, param);
				}
				case EntityRelationshipType.OneToOne:
				{
					var param = Expression.Parameter(typeof(T), "param");

					var relatedDataAccessObjectType = this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType());
					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					
					var leftExpressions = new List<Expression>();
					var rightExpressions = new List<Expression>();

					var leftPropertyNames = new List<string>();
					var rightPropertyNames = new List<string>();
					
					var concreteType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(relatedDataAccessObjectType);

					foreach (var primaryKey in newObjectTypeDescriptor.PrimaryKeyProperties)
					{
						var keyProp = concreteType.GetProperties().First(c => c.Name == this.PropertyName + primaryKey.PersistedShortName);

						var left = Expression.Property(param, primaryKey.PropertyInfo); 
						var right = Expression.Constant(keyProp.GetValue(this.RelatedDataAccessObject, null));

						leftExpressions.Add(left);
						rightExpressions.Add(right);

						leftPropertyNames.Add(primaryKey.PropertyName);
						rightPropertyNames.Add(keyProp.Name);
					}

					var body = Expression.Equal
					(
						new SqlObjectOperand(typeof(T), leftExpressions, leftPropertyNames),
						new SqlObjectOperand(typeof(T), rightExpressions, rightPropertyNames)
					);

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
			var initializeActionsStorage = this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache;

			Action<IDataAccessObject, IDataAccessObject> initializeDataAccessObject;

			if (initializeActionsStorage.initializeActions.TryGetValue(key, out initializeDataAccessObject))
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
					
					var newParam = Expression.Parameter(typeof(IDataAccessObject), "newobj");
					var relatedParam = Expression.Parameter(typeof(IDataAccessObject), "related");

					var propertyDescriptor = newObjectTypeDescriptor.RelatedProperties.Filter(c => relatedDataAccessObjectType.IsAssignableFrom(c.PropertyType)).First();

					var method = propertyDescriptor.PropertyInfo.GetSetMethod();

					var body = Expression.Call(Expression.Convert(newParam, typeof(T)), method, Expression.Convert(relatedParam, relatedDataAccessObjectType));

					var lambda = Expression.Lambda(body, relatedParam, newParam);

					this.InitializeDataAccessObject = (Action<IDataAccessObject, IDataAccessObject>)lambda.Compile();

					initializeActionsStorage.initializeActions[key] = this.InitializeDataAccessObject;

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

		IDataAccessObject IDataAccessObjectActivator.Create()
		{
			return this.Create();
		}
	}
}
