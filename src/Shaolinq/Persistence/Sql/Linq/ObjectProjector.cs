// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class ObjectProjector
	{
		public SqlQueryFormatResult FormatResult { get; private set; }
		public BaseDataAccessModel DataAccessModel { get; private set; }
		public PersistenceContext PersistenceContext { get; private set; }

		protected int count = 0;
		protected IDataReader dataReader;
		private readonly IQueryProvider provider;
		protected SelectFirstType selectFirstType;
		protected readonly IRelatedDataAccessObjectContext relatedDataAccessObjectContext;

		public ObjectProjector(IQueryProvider provider, BaseDataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, PersistenceContext persistenceContextStore, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType)
		{
			this.provider = provider;
			this.DataAccessModel = dataAccessModel;
			this.FormatResult = formatResult;
			this.PersistenceContext = persistenceContextStore;
			this.selectFirstType = selectFirstType;
			this.relatedDataAccessObjectContext = relatedDataAccessObjectContext;
		}

		public virtual IEnumerable<T> ExecuteSubQuery<T>(LambdaExpression query)
		{
			var projection = (SqlProjectionExpression)ExpressionReplacer.Replace
				(
					query.Body,
					query.Parameters[0],
					Expression.Constant(this)
				);
		
			projection = (SqlProjectionExpression) Evaluator.PartialEval(this.DataAccessModel, projection, CanEvaluateLocally);
            
			var result = (IEnumerable<T>) this.provider.Execute(projection);
			var list = new List<T>(result);

			if (typeof (IQueryable<T>).IsAssignableFrom(query.Body.Type))
			{
				return list.AsQueryable();
			}

			return list;
		}

		private static bool CanEvaluateLocally(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Parameter || (int)expression.NodeType > (int)SqlExpressionType.First)
			{
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Base class for ObjectReaders that use Reflection.Emit
	/// </summary>
	/// <typeparam name="T">
	/// The type of objects this projector returns
	/// </typeparam>
	/// <typeparam name="U">
	/// The concrete type for types this projector returns.  This type
	/// parameter is usually the same as <see cref="U"/> unless <see cref="T"/>
	/// is a <see cref="DataAccessObject{OBJECT_TYPE}"/> type in which case <see cref="U"/>
	/// must inherit from <see cref="T"/> and is usually automatically generated
	/// by the TypeBuilding system using Reflection.Emit.
	/// </typeparam>
	public class ObjectProjector<T, U>
		: ObjectProjector, IEnumerable<T>
		where U : T
	{
		protected readonly object[] placeholderValues;
		protected readonly Func<ObjectProjector, IDataReader, object[], U> objectReader;

		public ObjectProjector(IQueryProvider provider, BaseDataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, PersistenceContext persistenceContextStore, Delegate objectReader, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, object[] placeholderValues)
			: base(provider, dataAccessModel, formatResult, persistenceContextStore, relatedDataAccessObjectContext, selectFirstType)
		{
			this.placeholderValues = placeholderValues;
			this.objectReader = (Func<ObjectProjector, IDataReader, object[], U>)objectReader;
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.AmbientTransactionManager.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.PersistenceContext))
			{
				var persistenceTransactionContext = (SqlPersistenceTransactionContext)acquisition.PersistenceTransactionContext;

				using (this.dataReader = persistenceTransactionContext.ExecuteReader(this.FormatResult.CommandText, this.FormatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						if (count == 1 && this.selectFirstType == SelectFirstType.SingleOrDefault || this.selectFirstType == SelectFirstType.DefaultIfEmpty)
						{
							throw new InvalidOperationException("Sequence contains more than one element");
						}

						yield return this.objectReader(this, this.dataReader, placeholderValues);

						count++;
					}
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
