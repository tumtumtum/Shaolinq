// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class RelatedDataAccessObjects<T>
		: DataAccessObjectsQueryable<T>, IRelatedDataAccessObjectContext, IDataAccessObjectActivator, IHasCondition
		where T : DataAccessObject
	{
		private List<T> values;
		private HashSet<T> valuesSet;
		internal int valuesVersion;
		private IReadOnlyList<T> readOnlyValues;
		private readonly TypeRelationshipInfo relationshipInfo;
		public bool HasItems => this.values != null;
		public LambdaExpression Condition { get; protected set; }
		public IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create() => this.Create();
		public Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		
		public RelatedDataAccessObjects(DataAccessModel dataAccessModel, IDataAccessObjectAdvanced parentDataAccessObject, string parentPropertyName)
			: base(dataAccessModel)
		{
			this.RelatedDataAccessObject = parentDataAccessObject;
			
			var parentType = this.DataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(this.DataAccessModel.GetDefinitionTypeFromConcreteType(parentDataAccessObject.GetType()));
			this.relationshipInfo = parentType.GetRelationshipInfos().Single(c => c.ReferencingProperty.PropertyName == parentPropertyName);

			this.Condition = this.CreateJoinCondition(this.relationshipInfo.TargetProperty);
			this.InitializeDataAccessObject = this.GetInitializeRelatedMethod(parentType, this.relationshipInfo.TargetProperty);
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
				error = $"{(error == null ? "" : error + ". ")}Cache flushed because collection version {this.valuesVersion} did not match current DataAccessModel version {version}";

				this.values = null;
				this.valuesSet = null;
				this.readOnlyValues = null;

				return null;
			}

			return this.values;
		}

		/// <summary>
		/// Invalidates any eagerly loaded items.
		/// </summary>
		/// <returns></returns>
		public virtual RelatedDataAccessObjects<T> Invalidate()
		{
			this.values = null;
			this.valuesSet = null;
			this.valuesVersion = 0;
			this.readOnlyValues = null;

			return this;
		}

		/// <summary>
		/// Gets the eagerly loaded items in this collection or throws an <see cref="InvalidOperationException"/> if the collection hasn't been eaglerly loaded.
		/// </summary>
		/// <returns>
		/// A read-only list of the items in this collection.
		/// </returns>
		public virtual IReadOnlyList<T> Items()
		{
			return Items(false);
		}

		/// <summary>
		/// Gets the eagerly loaded items in this collection or throws an <see cref="InvalidOperationException"/>
		/// if the collection hasn't been eaglerly loaded or lazily loads the items and returns them if <paramref name="lazyLoadIfNecessary"/> is true.
		/// </summary>
		/// <param name="lazyLoadIfNecessary">If true then lazily loads the items if they haven't already been loaded (equivalent to using <see cref="LoadOptions.EagerOrLazy"/>) otherwise throws an exception if the items haven't already been loaded (equivalent to using <see cref="LoadOptions.EagerOnly"/>).</param>
		/// <returns>
		/// A read-only list of the items in this collection.
		/// </returns>
		public virtual IReadOnlyList<T> Items(bool lazyLoadIfNecessary)
		{
			return Items(lazyLoadIfNecessary ? LoadOptions.EagerOrLazy : LoadOptions.EagerOnly);
		}


		/// <summary>
		/// Gets the items in this collection using specified <see cref="LoadOptions"/>.
		/// </summary>
		/// <param name="options">Options that specify how to return values</param>
		/// <returns>The items</returns>
		public virtual IReadOnlyList<T> Items(LoadOptions options)
		{
			var isExplicitlyLazy = (options & (LoadOptions.LazyOnly | LoadOptions.EagerOnly)) == LoadOptions.LazyOnly;
			var notCachedAndLazy = (this.values == null && ((options & LoadOptions.LazyOnly) != 0));

			if (isExplicitlyLazy || notCachedAndLazy)
			{
				this.values = this.ToList();
			}

			var retval = this.values;

			if (retval == null)
			{
				throw new InvalidOperationException("No cached values available");
			}

			return retval;
		}

		internal void AddIfNotExist(T value)
		{
			if (!this.valuesSet.Contains(value))
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
					this.AddIfNotExist(value);
				}
			}
			else if (this.values.Count > 0)
			{
				if (value != null)
				{
					this.AddIfNotExist(value);
				}
			}
			else if (value != null)
			{
				this.AddIfNotExist(value);
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
			var key = this.relationshipInfo;
			var cache = this.DataAccessModel.relatedDataAccessObjectsInitializeActionsCache;
			
			if (cache.TryGetValue(key, out var initializeDataAccessObject))
			{
				return initializeDataAccessObject;
			}

			var childObject = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "childObject");
			var parentObject = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "parentObject");
			var body = Expression.Call(Expression.Convert(childObject, typeof(T)), childBackReferenceProperty.PropertyInfo.GetSetMethod(), Expression.Convert(parentObject, parentType.Type));
			var lambda = Expression.Lambda(body, parentObject, childObject);
			var retval = (Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>)lambda.Compile();

			var newCache = cache.Clone(key, retval, "InitializeRelatedMethod");

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
