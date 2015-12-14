// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ObjectProjector
	{
		public SqlQueryFormatResult FormatResult { get; }
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		protected int count = 0;
		protected readonly IQueryProvider provider;
		protected SelectFirstType selectFirstType;
		protected readonly SqlAggregateType? sqlAggregateType;
		protected readonly bool isDefaultIfEmpty;
		protected readonly IRelatedDataAccessObjectContext relatedDataAccessObjectContext;

		public ObjectProjector(IQueryProvider provider, DataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, SqlAggregateType? sqlAggregateType, bool isDefaultIfEmpty)
		{
			this.sqlAggregateType = sqlAggregateType;
			this.isDefaultIfEmpty = isDefaultIfEmpty;
			this.provider = provider;
			this.DataAccessModel = dataAccessModel;
			this.FormatResult = formatResult;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.selectFirstType = selectFirstType;
			this.relatedDataAccessObjectContext = relatedDataAccessObjectContext;
		}

		public virtual IEnumerable<T> ExecuteSubQuery<T>(LambdaExpression query, IDataReader dataReader)
		{
			var projection = (SqlProjectionExpression)query.Body;

			var expectedSelector = "GROUPBYCOLUMNS-" + projection.Select.Alias;

			projection = (SqlProjectionExpression)SqlExpressionReplacer.Replace(projection, c =>
			{
				if (query.Parameters[0] == c)
				{
					return Expression.Constant(this);
				}

				var column = c as SqlColumnExpression;
				
				if (column != null && column.SelectAlias.StartsWith(expectedSelector))
				{
					var sqlDataTypeProvider = this.SqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(column.Type);

					var parameter = Expression.Parameter(typeof(IDataReader));
					var func = Expression.Lambda<Func<IDataReader, object>>(Expression.Convert(sqlDataTypeProvider.GetReadExpression(parameter, dataReader.GetOrdinal(column.Name)), typeof(object)), parameter).Compile();

					var value = func(dataReader);

					return Expression.Constant(value, column.Type);
				}

				return null;
			});

			projection = (SqlProjectionExpression)SqlQueryProvider.Optimize(projection, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums(), true);

			return this.provider.CreateQuery<T>(projection);
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

		public ObjectProjector(IQueryProvider provider, DataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, SqlDatabaseContext sqlDatabaseContext, Delegate objectReader, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, SqlAggregateType? sqlAggregateType, bool isDefaultIfEmpty, object[] placeholderValues)
			: base(provider, dataAccessModel, formatResult, sqlDatabaseContext, relatedDataAccessObjectContext, selectFirstType, sqlAggregateType, isDefaultIfEmpty)
		{
			this.placeholderValues = placeholderValues;
			this.objectReader = (Func<ObjectProjector, IDataReader, object[], U>)objectReader;
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
			{
				var transactionalCommandsContext = (DefaultSqlTransactionalCommandsContext)acquisition.SqlDatabaseCommandsContext;

				using (var dataReader = transactionalCommandsContext.ExecuteReader(this.FormatResult.CommandText, this.FormatResult.ParameterValues))
				{
					if (dataReader.Read())
					{
						if (this.isDefaultIfEmpty && this.sqlAggregateType != null)
						{
							if (dataReader.FieldCount > 0 && dataReader.IsDBNull(0))
							{
								yield break;
							}
						}
						
						if (this.sqlAggregateType != SqlAggregateType.Sum && this.sqlAggregateType != SqlAggregateType.Count && this.sqlAggregateType != SqlAggregateType.LongCount && !typeof(T).IsNullableType())
						{
							if (dataReader.FieldCount > 0 && dataReader.IsDBNull(0))
							{
								throw new InvalidOperationException("Sequence contains no elements");
							}
						}
						
						if (this.isDefaultIfEmpty && (this.sqlAggregateType == SqlAggregateType.Count || this.sqlAggregateType == SqlAggregateType.LongCount))
						{
							if (dataReader.FieldCount > 0 && Convert.ToInt64(dataReader.GetValue(0)) == 0)
							{
								yield return (T)Convert.ChangeType(1, typeof(T));
							}
						}

						var value = this.objectReader(this, dataReader, this.placeholderValues);

						yield return value;

						this.count++;
					}

					while (dataReader.Read())
					{
						yield return this.objectReader(this, dataReader, this.placeholderValues);

						this.count++;
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
