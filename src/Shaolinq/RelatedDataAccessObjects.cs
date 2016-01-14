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
		private int valuesVersion;
		public RelationshipType RelationshipType { get; }
		public LambdaExpression ExtraCondition { get; protected set; }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create() => this.Create();
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		public override IEnumerator<T> GetEnumerator() => this.values?.GetEnumerator() ?? base.GetEnumerator();

		internal T PlaceholderItem { get; set; } = default(T);

		public RelatedDataAccessObjects(IDataAccessObjectAdvanced relatedDataAccessObject, DataAccessModel dataAccessModel, RelationshipType relationshipType)
			: base(dataAccessModel)
		{
			this.RelatedDataAccessObject = relatedDataAccessObject;
			this.RelationshipType = relationshipType;
			this.ExtraCondition = this.CreateJoinCondition();
			this.SqlQueryProvider.RelatedDataAccessObjectContext = this;
			this.InitializeDataAccessObject = this.GetInitializeRelatedMethod();
		}
		
		public virtual RelatedDataAccessObjects<T> Invalidate()
		{
			this.values = null;

			return this;
		}

		public virtual List<T> ToList(ToListCachePolicy cachePolicy = ToListCachePolicy.Default)
		{
			return new List<T>(this.AsEnumerable(cachePolicy));
		}

		public virtual IEnumerable<T> AsEnumerable(ToListCachePolicy cachePolicy = ToListCachePolicy.Default)
		{
			switch (cachePolicy)
			{
			case ToListCachePolicy.CacheOnly:
				return new List<T>(this.values);
			case ToListCachePolicy.IgnoreCache:
				return Enumerable.ToList(this);
			default:
				return this.values ?? (IEnumerable<T>)this;
			}
		}

		internal void Add(T value, int version)
		{
			if (this.values == null)
			{
				this.values = new List<T> { value };
			}
			else if (version != valuesVersion)
			{
				this.values.Clear();
				this.values.Add(value);
			}
            else if (values.Count > 0)
			{
				if (this.values.Last() != value)
				{
					this.values.Add(value);
				}
			}
			else
			{
				this.values.Add(value);
			}
		}

		private LambdaExpression CreateJoinCondition()
		{
			switch (this.RelationshipType)
			{
				case RelationshipType.ParentOfOneToMany:
				{
					var param = Expression.Parameter(typeof(T));

					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					var prop = newObjectTypeDescriptor.GetRelatedProperty(this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType()));
					var body = Expression.Equal(Expression.MakeMemberAccess(param, prop.PropertyInfo), Expression.Constant(this.RelatedDataAccessObject));

					return Expression.Lambda(body, param);
				}
				case RelationshipType.OneToOne:
				case RelationshipType.ChildOfOneToMany:
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
				case RelationshipType.ParentOfOneToMany:
				{
					var relatedDataAccessObjectType = this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.RelatedDataAccessObject.GetType());
					var newObjectTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(typeof(T));
					var newParam = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "newobj");
					var relatedParam = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "related");
					var propertyDescriptor = newObjectTypeDescriptor.RelationshipRelatedProperties.First(c => relatedDataAccessObjectType.IsAssignableFrom(c.PropertyType));
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
				case RelationshipType.ChildOfOneToMany:
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
