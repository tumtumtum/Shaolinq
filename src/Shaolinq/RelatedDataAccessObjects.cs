// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private IReadOnlyList<T> readOnlyValues;
		private int valuesVersion;
		public bool HasItems => AssertValues() != null;
		public LambdaExpression ExtraCondition { get; protected set; }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create() => this.Create();
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		public override IEnumerator<T> GetEnumerator() => this.AssertValues()?.GetEnumerator() ?? base.GetEnumerator();
		public virtual int Count() => this.AssertValues()?.Count ?? Queryable.Count(this);
		
		public RelatedDataAccessObjects(DataAccessModel dataAccessModel, IDataAccessObjectAdvanced parentDataAccessObject, string parentPropertyName)
			: base(dataAccessModel)
		{
			this.RelatedDataAccessObject = parentDataAccessObject;
			this.SqlQueryProvider.RelatedDataAccessObjectContext = this;
			
			var parentType = this.DataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(this.DataAccessModel.GetDefinitionTypeFromConcreteType(parentDataAccessObject.GetType()));
			var relationshipInfo = parentType.GetRelationshipInfos().Single(c => c.ReferencingProperty.PropertyName == parentPropertyName);

			this.ExtraCondition = this.CreateJoinCondition(relationshipInfo.TargetProperty);
			this.InitializeDataAccessObject = this.GetInitializeRelatedMethod(parentType, relationshipInfo.TargetProperty);
		}

		private IReadOnlyList<T> AssertValues()
		{
			if (readOnlyValues == null)
			{
				return null;
			}

			if (valuesVersion != this.DataAccessModel.GetCurrentContext(false).GetCurrentVersion())
			{
				this.values = null;
				this.readOnlyValues = null;

				return null;
			}

			return this.values;
		}

		public virtual RelatedDataAccessObjects<T> Invalidate()
		{
			this.values = null;
			this.valuesVersion = 0;
			this.readOnlyValues = null;

			return this;
		}

		public virtual IReadOnlyList<T> Items()
		{
			var retval = this.AssertValues();

			if (retval == null)
			{
				throw new InvalidOperationException("No cached values available");
			}

			return retval;
		}

		internal void Add(T value, int version)
		{
			if (value == null)
			{
				if (this.values == null)
				{
					valuesVersion = version;

					this.values = new List<T>();
					this.readOnlyValues = new ReadOnlyCollection<T>(this.values);
				}

				return;
			}

			if (this.values == null)
			{
				valuesVersion = version;

				this.values = new List<T> { value };
				this.readOnlyValues = new ReadOnlyCollection<T>(this.values);
			}
			else if (version != valuesVersion)
			{
				valuesVersion = version;

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

		private LambdaExpression CreateJoinCondition(PropertyDescriptor childBackReferenceProperty)
		{
			var param = Expression.Parameter(typeof(T));
			var body = Expression.Equal(Expression.MakeMemberAccess(param, childBackReferenceProperty), Expression.Constant(this.RelatedDataAccessObject));

			return Expression.Lambda(body, param);
		}
		
		private Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> GetInitializeRelatedMethod(TypeDescriptor parentType, PropertyDescriptor childBackReferenceProperty)
		{
			var key = new Tuple<Type, Type>(this.RelatedDataAccessObject.GetType(), typeof(T));
			var cache = this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache;

			Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> initializeDataAccessObject;

			if (cache.TryGetValue(key, out initializeDataAccessObject))
			{
				return initializeDataAccessObject;
			}

			var childObject = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "childObject");
			var parentObject = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "parentObject");
			var body = Expression.Call(Expression.Convert(childObject, typeof(T)), childBackReferenceProperty.PropertyInfo.GetSetMethod(), Expression.Convert(parentObject, parentType.Type));
			var lambda = Expression.Lambda(body, parentObject, childObject);
			var retval = (Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>)lambda.Compile();
			
			var newCache = new Dictionary<Tuple<Type, Type>, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(cache)
			{
				[key] = retval
			};

			this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache = newCache;

			return retval;
		}

		public override T Create()
		{
			var retval = base.Create();

			this.InitializeDataAccessObject?.Invoke(this.RelatedDataAccessObject, retval);

			return retval;
		}
	}
}
