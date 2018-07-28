// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence
{
	public abstract partial class SqlTransactionalCommandsContext
		: IDisposable
	{
		public TransactionContext TransactionContext { get; set; }
		internal MarsDataReader currentReader;

		private bool disposed;
		public bool SupportsAsync { get; protected set; }
		public IDbConnection DbConnection { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		protected IDbTransaction dbTransaction;
		public DataAccessModel DataAccessModel { get; }
		private readonly bool emulateMultipleActiveResultSets;
		private readonly string parameterIndicatorPrefix;

		public abstract void Delete(SqlDeleteExpression deleteExpression);
		public abstract Task DeleteAsync(SqlDeleteExpression deleteExpression);
		public abstract Task DeleteAsync(SqlDeleteExpression deleteExpression, CancellationToken cancellationToken);
		public abstract void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken);

		public abstract void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken);

		public abstract InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects);
		public abstract Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken);

		public abstract int ExecuteNonQuery(string sql, IReadOnlyList<TypedValue> parameters);
		public abstract Task<int> ExecuteNonQueryAsync(string sql, IReadOnlyList<TypedValue> parameters);
		public abstract Task<int> ExecuteNonQueryAsync(string sql, IReadOnlyList<TypedValue> parameters, CancellationToken cancellationToken);

		public abstract ExecuteReaderContext ExecuteReader(string sql, IReadOnlyList<TypedValue> parameters);
		public abstract Task<ExecuteReaderContext> ExecuteReaderAsync(string sql, IReadOnlyList<TypedValue> parameters);
		public abstract Task<ExecuteReaderContext> ExecuteReaderAsync(string sql, IReadOnlyList<TypedValue> parameters, CancellationToken cancellationToken);

		public virtual bool IsClosed => this.DbConnection == null || this.DbConnection?.State == ConnectionState.Closed || this.DbConnection?.State == ConnectionState.Broken;

		public void FillParameters(IDbCommand command, SqlQueryFormatResult formatResult)
		{
			command.Parameters.Clear();

			foreach (var parameter in formatResult.ParameterValues)
			{
				AddParameter(command, parameter.Type, parameter.Name, parameter.Value);
			}
		}

		public IDbDataParameter AddParameter(IDbCommand command, Type type, object value)
		{
			return AddParameter(command, type, null, value);
		}

		public IDbDataParameter AddParameter(IDbCommand command, Type type, string name, object value)
		{
			var parameter = CreateParameter(command,  name ?? this.parameterIndicatorPrefix + SqlQueryFormatter.ParamNamePrefix + command.Parameters.Count, type, value);

			command.Parameters.Add(parameter);

			return parameter;
		}

		protected virtual DbType GetDbType(Type type)
		{
			type = type.GetUnwrappedNullableType();

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return DbType.Boolean;
				case TypeCode.Byte:
				case TypeCode.SByte:
					return DbType.Byte;
				case TypeCode.Char:
					return DbType.Object;
				case TypeCode.DateTime:
					return DbType.DateTime;
				case TypeCode.Decimal:
					return DbType.Decimal;
				case TypeCode.Single:
					return DbType.Single;
				case TypeCode.Double:
					return DbType.Double;
				case TypeCode.Int16:
				case TypeCode.UInt16:
					return DbType.Int16;
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return DbType.Int32;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return DbType.Int64;
				case TypeCode.String:
					return DbType.AnsiString;
				default:
					if (type == typeof(Guid))
					{
						return DbType.Guid;
					}
					else if (type.IsArray && type.GetElementType() == typeof(byte))
					{
						return DbType.Binary;
					}
					else if (type.IsEnum)
					{
						return DbType.AnsiString;
					}

					return DbType.Object;
			}
		}

		protected virtual IDbDataParameter CreateParameter(IDbCommand command, string parameterName, Type type, object value)
		{
			var parameter = command.CreateParameter();

			parameter.ParameterName = parameterName;

			if (value == null)
			{
				parameter.DbType = GetDbType(type);
			}

			var result = this.SqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(type).ConvertForSql(value);

			parameter.DbType = GetDbType(result.Type);
			parameter.Value = result.Value ?? DBNull.Value;

			return parameter;
		}
		
		public static IsolationLevel ConvertIsolationLevel(DataAccessIsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
			case DataAccessIsolationLevel.ReadUncommitted:
				return IsolationLevel.ReadUncommitted;
			case DataAccessIsolationLevel.Serializable:
				return IsolationLevel.Serializable;
			case DataAccessIsolationLevel.ReadCommitted:
				return IsolationLevel.ReadCommitted;
			case DataAccessIsolationLevel.Chaos:
				return IsolationLevel.Chaos;
			case DataAccessIsolationLevel.RepeatableRead:
				return IsolationLevel.RepeatableRead;
			case DataAccessIsolationLevel.Snapshot:
				return IsolationLevel.Snapshot;
			default:
				return IsolationLevel.Unspecified;
			}
		}

		protected SqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection dbConnection, TransactionContext transactionContext)
		{
			this.TransactionContext = transactionContext;

			try
		    {
		        this.DbConnection = dbConnection;
		        this.SqlDatabaseContext = sqlDatabaseContext;
		        this.DataAccessModel = sqlDatabaseContext.DataAccessModel;
			    this.parameterIndicatorPrefix = this.SqlDatabaseContext.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);

				this.emulateMultipleActiveResultSets = !sqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.MultipleActiveResultSets);

		        if (transactionContext?.DataAccessTransaction != null)
		        {
		            this.dbTransaction = dbConnection.BeginTransaction(ConvertIsolationLevel(transactionContext.DataAccessTransaction.IsolationLevel));
		        }
		    }
		    catch
		    {
		        Dispose(true);

		        throw;
		    }
		}

		~SqlTransactionalCommandsContext()
		{
			Dispose(false);
		}

		public virtual IDbCommand CreateCommand() => CreateCommand(SqlCreateCommandOptions.Default);

		public virtual IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = this.DbConnection.CreateCommand();

			 retval.Transaction = this.dbTransaction;

			if (this.SqlDatabaseContext.CommandTimeout != null)
			{
				retval.CommandTimeout = (int)this.SqlDatabaseContext.CommandTimeout.Value.TotalMilliseconds;
			}

			if (this.emulateMultipleActiveResultSets)
			{
				retval = new MarsDbCommand(this, retval);
			}

			return retval;
		}

		public virtual void Prepare()
		{
			throw new NotSupportedException();
		}

		[RewriteAsync]
		public virtual void Commit()
		{
			try
			{
				if (this.dbTransaction != null)
				{
					this.dbTransaction.Commit();
					this.dbTransaction.Dispose();
					this.dbTransaction = null;
				}
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}
			finally
			{
				CloseConnection();
			}
		}

		[RewriteAsync]
		public virtual void Rollback()
		{
			try
			{
				if (this.dbTransaction != null)
				{
					this.dbTransaction.Rollback();
					this.dbTransaction.Dispose();
					this.dbTransaction = null;
				}
			}
			finally
			{
				CloseConnection();
			}
		}

		protected virtual void CloseConnection()
		{
			try { this.currentReader?.Close(); } catch { }
			try { this.currentReader?.Dispose(); } catch { }
			try { this.dbTransaction?.Dispose(); } catch { }
			try { this.DbConnection?.Close(); } catch { }

			this.DbConnection = null;
			this.dbTransaction = null;
			this.currentReader = null;

			GC.SuppressFinalize(this);
		}

		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}

			try { CloseConnection(); } catch { }

			this.disposed = true;
		}
	}
}
