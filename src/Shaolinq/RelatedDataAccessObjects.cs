// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
		private HashSet<T> valuesSet;
		private int valuesVersion;
		private IReadOnlyList<T> readOnlyValues;
		private readonly TypeRelationshipInfo relationshipInfo;
		public bool HasItems => this.AssertValues() != null;
		public LambdaExpression ExtraCondition { get; protected set; }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create() => this.Create();
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		
		public RelatedDataAccessObjects(DataAccessModel dataAccessModel, IDataAccessObjectAdvanced parentDataAccessObject, string parentPropertyName)
			: base(dataAccessModel)
		{
			this.RelatedDataAccessObject = parentDataAccessObject;
			this.SqlQueryProvider.RelatedDataAccessObjectContext = this;
			
			var parentType = this.DataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(this.DataAccessModel.GetDefinitionTypeFromConcreteType(parentDataAccessObject.GetType()));
			this.relationshipInfo = parentType.GetRelationshipInfos().Single(c => c.ReferencingProperty.PropertyName == parentPropertyName);

			this.ExtraCondition = this.CreateJoinCondition(relationshipInfo.TargetProperty);
			this.InitializeDataAccessObject = this.GetInitializeRelatedMethod(parentType, relationshipInfo.TargetProperty);
		}

		private IReadOnlyList<T> AssertValues()
		{
			var error = "";

			return AssertValues(ref error);
		}

		private IReadOnlyList<T> AssertValues(ref string error)
		{
			if (this.readOnlyValues == null)
			{
				return null;
			}

			var version = TransactionContext.GetCurrentTransactionContextVersion(this.DataAccessModel);

			if (this.valuesVersion != version)
			{
				error = $"{(error == null ? "" : error + ". ")}Cache flushed because collection version {valuesVersion} did not match current DataAccessModel version {version}";

				this.values = null;
				this.valuesSet = null;
				this.readOnlyValues = null;

				return null;
			}

			return this.values;
		}

		public virtual RelatedDataAccessObjects<T> Invalidate()
		{
			this.values = null;
			this.valuesSet = null;
			this.valuesVersion = 0;
			this.readOnlyValues = null;

			return this;
		}

		public virtual IReadOnlyList<T> Items()
		{
			var error = "No cached values available";

			var retval = this.AssertValues(ref error);

			if (retval == null)
			{
				throw new InvalidOperationException(error);
			}

			return retval;
		}

		internal void AddIfNotExist(T value)
		{
			if (!valuesSet.Contains(value))
			{
				this.values.Add(value);
				this.valuesSet.Add(value);
			}
		}

		internal void Add(T value, int version)
		{
			if (this.values == null)
			{
				this.valuesVersion = version;

				this.values = value == null ? new List<T>() : new List<T> { value };
				this.valuesSet = new HashSet<T>(this.values);
				this.readOnlyValues = new ReadOnlyCollection<T>(this.values);
			}
			else if (version != this.valuesVersion)
			{
				this.valuesVersion = version;

				this.values.Clear();
				this.valuesSet.Clear();

				if (value != null)
				{
					AddIfNotExist(value);
				}
			}
            else if (this.values.Count > 0)
			{
				if (value != null)
				{
					AddIfNotExist(value);
				}
			}
			else if (value != null)
			{
				AddIfNotExist(value);
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
			var key = relationshipInfo;
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
			
			var newCache = new Dictionary<TypeRelationshipInfo, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(cache)
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
