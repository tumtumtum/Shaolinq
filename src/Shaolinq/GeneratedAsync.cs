namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Transactions;
	using System.Threading.Tasks;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	public partial class DataAccessScope
	{
		public Task FlushAsync()
		{
			return FlushAsync(CancellationToken.None);
		}

		public async Task FlushAsync(CancellationToken cancellationToken)
		{
			this.transaction.CheckAborted();
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					await dataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
				}
			}
		}

		public Task FlushAsync(DataAccessModel dataAccessModel)
		{
			return FlushAsync(dataAccessModel, CancellationToken.None);
		}

		public async Task FlushAsync(DataAccessModel dataAccessModel, CancellationToken cancellationToken)
		{
			this.transaction.CheckAborted();
			if (!dataAccessModel.IsDisposed)
			{
				await dataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		public Task CompleteAsync()
		{
			return CompleteAsync(CancellationToken.None);
		}

		public async Task CompleteAsync(CancellationToken cancellationToken)
		{
			await this.CompleteAsync(ScopeCompleteOptions.Default, cancellationToken).ConfigureAwait(false);
		}

		public Task CompleteAsync(ScopeCompleteOptions options)
		{
			return CompleteAsync(options, CancellationToken.None);
		}

		public async Task CompleteAsync(ScopeCompleteOptions options, CancellationToken cancellationToken)
		{
			this.complete = true;
			this.transaction?.CheckAborted();
			if ((options & ScopeCompleteOptions.SuppressAutoFlush) != 0)
			{
				await this.FlushAsync(cancellationToken).ConfigureAwait(false);
			}

			if (this.transaction == null)
			{
				DataAccessTransaction.Current = this.outerTransaction;
				return;
			}

			if (!this.isRoot)
			{
				DataAccessTransaction.Current = this.outerTransaction;
				return;
			}

			if (this.transaction.HasSystemTransaction)
			{
				return;
			}

			if (this.transaction != DataAccessTransaction.Current)
			{
				throw new InvalidOperationException($"Cannot commit {this.GetType().Name} within another Async/Call context");
			}

			await this.transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
			this.transaction.Dispose();
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Linq;
	using System.Threading;
	using System.Transactions;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using Platform;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	public partial class DataAccessTransaction
	{
		public Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public async Task CommitAsync(CancellationToken cancellationToken)
		{
			this.isfinishing = true;
			if (this.transactionContextsByDataAccessModel != null)
			{
				foreach (var transactionContext in this.transactionContextsByDataAccessModel.Values)
				{
					await transactionContext.CommitAsync(cancellationToken).ConfigureAwait(false);
					transactionContext.Dispose();
				}
			}
		}

		public Task RollbackAsync()
		{
			return RollbackAsync(CancellationToken.None);
		}

		public async Task RollbackAsync(CancellationToken cancellationToken)
		{
			this.isfinishing = true;
			this.aborted = true;
			if (this.transactionContextsByDataAccessModel != null)
			{
				foreach (var transactionContext in this.transactionContextsByDataAccessModel.Values)
				{
					ActionUtils.IgnoreExceptions(() => transactionContext.Rollback());
				}
			}
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Collections;
	using System.Threading.Tasks;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	internal partial class DefaultIfEmptyEnumerator<T>
	{
		public Task<bool> MoveNextAsync()
		{
			return MoveNextAsync(CancellationToken.None);
		}

		public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			switch (this.state)
			{
				case 0:
					goto state0;
				case 1:
					goto state1;
				case 9:
					goto state9;
			}

			state0:
				var result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (!result)
			{
				this.Current = this.specifiedValue;
				this.state = 9;
				return true;
			}
			else
			{
				this.state = 1;
				this.Current = this.enumerator.Current;
				return true;
			}

			state1:
				result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (result)
			{
				this.Current = this.enumerator.Current;
				return true;
			}
			else
			{
				this.state = 9;
				return false;
			}

			state9:
				return false;
		}
	}

	internal partial class DefaultIfEmptyCoalesceSpecifiedValueEnumerator<T>
	{
		public Task<bool> MoveNextAsync()
		{
			return MoveNextAsync(CancellationToken.None);
		}

		public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			switch (this.state)
			{
				case 0:
					goto state0;
				case 1:
					goto state1;
				case 9:
					goto state9;
			}

			state0:
				var result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (!result)
			{
				this.Current = this.specifiedValue ?? default (T);
				this.state = 9;
				return true;
			}
			else
			{
				this.state = 1;
				this.Current = this.enumerator.Current;
				return true;
			}

			state1:
				result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (result)
			{
				this.Current = this.enumerator.Current;
				return true;
			}
			else
			{
				this.state = 9;
				return false;
			}

			state9:
				return false;
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Collections;
	using System.Threading.Tasks;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	internal partial class EmptyIfFirstIsNullEnumerator<T>
	{
		public Task<bool> MoveNextAsync()
		{
			return MoveNextAsync(CancellationToken.None);
		}

		public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			switch (this.state)
			{
				case 0:
					goto state0;
				case 1:
					goto state1;
				case 9:
					goto state9;
			}

			state0:
				var result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (!result || this.enumerator.Current == null)
			{
				this.state = 9;
				return false;
			}
			else
			{
				this.state = 1;
				this.Current = this.enumerator.Current;
				return true;
			}

			state1:
				result = (await EnumerableExtensions.MoveNextAsync(this.enumerator, cancellationToken).ConfigureAwait(false));
			if (result)
			{
				this.Current = this.enumerator.Current;
				return true;
			}
			else
			{
				this.state = 9;
				return false;
			}

			state9:
				return false;
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Linq;
	using System.Threading;
	using System.Reflection;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Runtime.CompilerServices;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	public static partial class EnumerableExtensions
	{
		internal static Task<T> AlwaysReadFirstAsync<T>(this IEnumerable<T> source)
		{
			return AlwaysReadFirstAsync(source, CancellationToken.None);
		}

		internal static async Task<T> AlwaysReadFirstAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return await source.FirstAsync(cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> CountAsync<T>(this IEnumerable<T> source)
		{
			return CountAsync(source, CancellationToken.None);
		}

		public static async Task<int> CountAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var list = source as ICollection<T>;
			if (list != null)
			{
				return list.Count;
			}

			var retval = 0;
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				while (enumerator.MoveNext())
				{
					retval++;
				}
			}

			return retval;
		}

		public static Task<long> LongCountAsync<T>(this IEnumerable<T> source)
		{
			return LongCountAsync(source, CancellationToken.None);
		}

		public static async Task<long> LongCountAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var list = source as ICollection<T>;
			if (list != null)
			{
				return list.Count;
			}

			var retval = 0L;
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				while (enumerator.MoveNext())
				{
					retval++;
				}
			}

			return retval;
		}

		internal static Task<T> SingleOrSpecifiedValueIfFirstIsDefaultValueAsync<T>(this IEnumerable<T> source, T specifiedValue)
		{
			return SingleOrSpecifiedValueIfFirstIsDefaultValueAsync(source, specifiedValue, CancellationToken.None);
		}

		internal static async Task<T> SingleOrSpecifiedValueIfFirstIsDefaultValueAsync<T>(this IEnumerable<T> source, T specifiedValue, CancellationToken cancellationToken)
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.Single<T>(Enumerable.Empty<T>());
				}

				var result = enumerator.Current;
				if (enumerator.MoveNext())
				{
					return Enumerable.Single<T>(new T[2]);
				}

				if (object.Equals(result, default (T)))
				{
					return specifiedValue;
				}

				return result;
			}
		}

		public static Task<T> SingleAsync<T>(this IEnumerable<T> source)
		{
			return SingleAsync(source, CancellationToken.None);
		}

		public static async Task<T> SingleAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.Single<T>(Enumerable.Empty<T>());
				}

				var result = enumerator.Current;
				if (enumerator.MoveNext())
				{
					return Enumerable.Single<T>(new T[2]);
				}

				return result;
			}
		}

		public static Task<T> SingleOrDefaultAsync<T>(this IEnumerable<T> source)
		{
			return SingleOrDefaultAsync(source, CancellationToken.None);
		}

		public static async Task<T> SingleOrDefaultAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext())
				{
					return default (T);
				}

				var result = enumerator.Current;
				if (enumerator.MoveNext())
				{
					return Enumerable.Single(new T[2]);
				}

				return result;
			}
		}

		public static Task<T> FirstAsync<T>(this IEnumerable<T> enumerable)
		{
			return FirstAsync(enumerable, CancellationToken.None);
		}

		public static async Task<T> FirstAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(enumerable).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.First(Enumerable.Empty<T>());
				}

				return enumerator.Current;
			}
		}

		public static Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> source)
		{
			return FirstOrDefaultAsync(source, CancellationToken.None);
		}

		public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext())
				{
					return default (T);
				}

				return enumerator.Current;
			}
		}

		internal static Task<T> SingleOrExceptionIfFirstIsNullAsync<T>(this IEnumerable<T? > source)where T : struct
		{
			return SingleOrExceptionIfFirstIsNullAsync(source, CancellationToken.None);
		}

		internal static async Task<T> SingleOrExceptionIfFirstIsNullAsync<T>(this IEnumerable<T? > source, CancellationToken cancellationToken)where T : struct
		{
			using (var enumerator = (await EnumerableExtensions.GetEnumeratorAsync(source).ConfigureAwait(false)))
			{
				if (!enumerator.MoveNext() || enumerator.Current == null)
				{
					throw new InvalidOperationException("Sequence contains no elements");
				}

				return enumerator.Current.Value;
			}
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
	using System.Data;
	using System.Threading;
	using System.Data.Common;
	using System.Threading.Tasks;
	using Shaolinq;
	using Shaolinq.Persistence;

	public static partial class DbCommandExtensions
	{
		public static Task<IDataReader> ExecuteReaderExAsync(this IDbCommand command, DataAccessModel dataAccessModel, bool suppressAnalytics = false)
		{
			return ExecuteReaderExAsync(command, dataAccessModel, CancellationToken.None, suppressAnalytics);
		}

		public static async Task<IDataReader> ExecuteReaderExAsync(this IDbCommand command, DataAccessModel dataAccessModel, CancellationToken cancellationToken, bool suppressAnalytics = false)
		{
			var marsDbCommand = command as MarsDbCommand;
			if (marsDbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return await marsDbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			}

			var dbCommand = command as DbCommand;
			if (dbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			}

			return command.ExecuteReader();
		}

		public static Task<int> ExecuteNonQueryExAsync(this IDbCommand command, DataAccessModel dataAccessModel, bool suppressAnalytics = false)
		{
			return ExecuteNonQueryExAsync(command, dataAccessModel, CancellationToken.None, suppressAnalytics);
		}

		public static async Task<int> ExecuteNonQueryExAsync(this IDbCommand command, DataAccessModel dataAccessModel, CancellationToken cancellationToken, bool suppressAnalytics = false)
		{
			var marsDbCommand = command as MarsDbCommand;
			if (marsDbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return await marsDbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}

			var dbCommand = command as DbCommand;
			if (dbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}

			return command.ExecuteNonQuery();
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
	using System.Data;
	using System.Threading;
	using System.Reflection;
	using System.Data.Common;
	using System.Threading.Tasks;
	using Shaolinq;
	using Shaolinq.Persistence;

	public static partial class DataReaderExtensions
	{
		public static Task<bool> ReadExAsync(this IDataReader reader)
		{
			return ReadExAsync(reader, CancellationToken.None);
		}

		public static async Task<bool> ReadExAsync(this IDataReader reader, CancellationToken cancellationToken)
		{
			var dbDataReader = reader as DbDataReader;
			if (dbDataReader != null)
			{
				return await dbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false);
			}

			return reader.Read();
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using System.Collections.Generic;
	using Shaolinq;
	using Shaolinq.Logging;
	using Shaolinq.Persistence;
	using Shaolinq.TypeBuilding;
	using Shaolinq.Persistence.Linq;
	using Shaolinq.Persistence.Linq.Expressions;

	public partial class DefaultSqlTransactionalCommandsContext
	{
		public override Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<TypedValue> parameters)
		{
			return ExecuteReaderAsync(sql, parameters, CancellationToken.None);
		}

		public override async Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<TypedValue> parameters, CancellationToken cancellationToken)
		{
			using (var command = this.CreateCommand())
			{
				foreach (var value in parameters)
				{
					this.AddParameter(command, value.Type, value.Value);
				}

				command.CommandText = sql;
				Logger.Info(() => this.FormatCommand(command));
				try
				{
					return await command.ExecuteReaderExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					var decoratedException = LogAndDecorateException(e, command);
					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}

		public override Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			return UpdateAsync(type, dataAccessObjects, CancellationToken.None);
		}

		public override async Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			await UpdateAsync(type, dataAccessObjects, true, cancellationToken).ConfigureAwait(false);
		}

		private Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, bool resetModified)
		{
			return UpdateAsync(type, dataAccessObjects, resetModified, CancellationToken.None);
		}

		private async Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, bool resetModified, CancellationToken cancellationToken)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;
				if ((objectState & (DataAccessObjectState.Changed | DataAccessObjectState.ServerSidePropertiesHydrated)) == 0)
				{
					continue;
				}

				using (var command = this.BuildUpdateCommand(typeDescriptor, dataAccessObject))
				{
					if (command == null)
					{
						Logger.ErrorFormat("Object is reported as changed but GetChangedProperties returns an empty list ({0})", dataAccessObject);
						continue;
					}

					Logger.Info(() => this.FormatCommand(command));
					int result;
					try
					{
						result = (await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false));
					}
					catch (Exception e)
					{
						var decoratedException = LogAndDecorateException(e, command);
						if (decoratedException != null)
						{
							throw decoratedException;
						}

						throw;
					}

					if (result == 0)
					{
						throw new MissingDataAccessObjectException(dataAccessObject, null, command.CommandText);
					}

					if (resetModified)
					{
						dataAccessObject.ToObjectInternal().ResetModified();
					}
				}
			}
		}

		public override Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			return InsertAsync(type, dataAccessObjects, CancellationToken.None);
		}

		public override async Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			var listToFixup = new List<DataAccessObject>();
			var listToRetry = new List<DataAccessObject>();
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;
				switch (objectState & DataAccessObjectState.NewChanged)
				{
					case DataAccessObjectState.Unchanged:
						continue;
					case DataAccessObjectState.New:
					case DataAccessObjectState.NewChanged:
						break;
					case DataAccessObjectState.Changed:
						throw new NotSupportedException("Changed state not supported");
				}

				var primaryKeyIsComplete = (objectState & DataAccessObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) != DataAccessObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties;
				var deferrableOrNotReferencingNewObject = (this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.Deferrability) || ((objectState & DataAccessObjectState.ReferencesNewObject) == 0));
				var objectReadyToBeCommited = primaryKeyIsComplete && deferrableOrNotReferencingNewObject;
				if (objectReadyToBeCommited)
				{
					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
					using (var command = this.BuildInsertCommand(typeDescriptor, dataAccessObject))
					{
						Logger.Info(() => this.FormatCommand(command));
						try
						{
							var reader = (await command.ExecuteReaderExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false));
							using (reader)
							{
								if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
								{
									var dataAccessObjectInternal = dataAccessObject.ToObjectInternal();
									var result = (await reader.ReadExAsync(cancellationToken).ConfigureAwait(false));
									if (result)
									{
										this.ApplyPropertiesGeneratedOnServerSide(dataAccessObject, reader);
										dataAccessObjectInternal.MarkServerSidePropertiesAsApplied();
									}

									reader.Close();
									if (dataAccessObjectInternal.ComputeServerGeneratedIdDependentComputedTextProperties())
									{
										await this.UpdateAsync(dataAccessObject.GetType(), new[]{dataAccessObject}, false, cancellationToken).ConfigureAwait(false);
									}
								}
							}
						}
						catch (Exception e)
						{
							var decoratedException = LogAndDecorateException(e, command);
							if (decoratedException != null)
							{
								throw decoratedException;
							}

							throw;
						}

						if ((objectState & DataAccessObjectState.ReferencesNewObjectWithServerSideProperties) == DataAccessObjectState.ReferencesNewObjectWithServerSideProperties)
						{
							listToFixup.Add(dataAccessObject);
						}
						else
						{
							dataAccessObject.ToObjectInternal().ResetModified();
						}
					}
				}
				else
				{
					listToRetry.Add(dataAccessObject);
				}
			}

			return new InsertResults(listToFixup, listToRetry);
		}

		public override Task DeleteAsync(SqlDeleteExpression deleteExpression)
		{
			return DeleteAsync(deleteExpression, CancellationToken.None);
		}

		public override async Task DeleteAsync(SqlDeleteExpression deleteExpression, CancellationToken cancellationToken)
		{
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(deleteExpression, SqlQueryFormatterOptions.Default);
			using (var command = this.CreateCommand())
			{
				command.CommandText = formatResult.CommandText;
				foreach (var value in formatResult.ParameterValues)
				{
					this.AddParameter(command, value.Type, value.Value);
				}

				Logger.Info(() => this.FormatCommand(command));
				try
				{
					var count = (await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false));
				}
				catch (Exception e)
				{
					var decoratedException = LogAndDecorateException(e, command);
					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}

		public override Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			return DeleteAsync(type, dataAccessObjects, CancellationToken.None);
		}

		public override async Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
			var parameter = Expression.Parameter(typeDescriptor.Type, "value");
			Expression body = null;
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var currentExpression = Expression.Equal(parameter, Expression.Constant(dataAccessObject));
				if (body == null)
				{
					body = currentExpression;
				}
				else
				{
					body = Expression.OrElse(body, currentExpression);
				}
			}

			if (body == null)
			{
				return;
			}

			var condition = Expression.Lambda(body, parameter);
			var expression = (Expression)Expression.Call(Expression.Constant(this.DataAccessModel), MethodInfoFastRef.DataAccessModelGetDataAccessObjectsMethod.MakeGenericMethod(typeDescriptor.Type));
			expression = Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeDescriptor.Type), expression, Expression.Quote(condition));
			expression = Expression.Call(MethodInfoFastRef.QueryableExtensionsDeleteMethod.MakeGenericMethod(typeDescriptor.Type), expression);
			var provider = new SqlQueryProvider(this.DataAccessModel, this.SqlDatabaseContext);
			await SqlQueryProviderExtensions.ExecuteAsync<int>(((ISqlQueryProvider)provider), expression, cancellationToken).ConfigureAwait(false);
		}
	}
}

namespace Shaolinq.Persistence.Linq
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Collections;
	using System.Threading.Tasks;
	using Shaolinq;
	using Shaolinq.Persistence;
	using Shaolinq.Persistence.Linq;

	internal partial class ObjectProjectionAsyncEnumerator<T, U, C>
	{
		public virtual Task<bool> MoveNextAsync()
		{
			return MoveNextAsync(CancellationToken.None);
		}

		public virtual async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			switch (state)
			{
				case 0:
					goto state0;
				case 1:
					goto state1;
				case 9:
					goto state9;
			}

			state0:
				this.state = 1;
			this.dataReader = (await this.persistenceAcquisition.SqlDatabaseCommandsContext.ExecuteReaderAsync(this.objectProjector.formatResult.CommandText, this.objectProjector.formatResult.ParameterValues, cancellationToken).ConfigureAwait(false));
			this.context = objectProjector.CreateEnumerationContext(this.dataReader, this.transactionContextAcquisition.Version);
			state1:
				T result;
			if (await this.dataReader.ReadExAsync(cancellationToken).ConfigureAwait(false))
			{
				T value = this.objectProjector.objectReader(this.objectProjector, this.dataReader, this.transactionContextAcquisition.Version, this.objectProjector.placeholderValues, o => objectProjector.ProcessDataAccessObject(o, ref context));
				if (this.objectProjector.ProcessMoveNext(this.dataReader, value, ref this.context, out result))
				{
					this.Current = result;
					return true;
				}

				goto state1;
			}

			this.state = 9;
			if (this.objectProjector.ProcessLastMoveNext(this.dataReader, ref this.context, out result))
			{
				this.Current = result;
				return true;
			}

			state9:
				return false;
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Data.Common;
	using System.Threading.Tasks;
	using Shaolinq;
	using Shaolinq.Persistence;

	public partial class MarsDbCommand
	{
		public virtual Task<int> ExecuteNonQueryAsync()
		{
			return ExecuteNonQueryAsync(CancellationToken.None);
		}

		public async virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			this.context.currentReader?.BufferAll();
			var dbCommand = this.Inner as DbCommand;
			if (dbCommand != null)
			{
				return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				return base.ExecuteNonQuery();
			}
		}

		public virtual Task<object> ExecuteScalarAsync()
		{
			return ExecuteScalarAsync(CancellationToken.None);
		}

		public async virtual Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			this.context.currentReader?.BufferAll();
			var dbCommand = this.Inner as DbCommand;
			if (dbCommand != null)
			{
				return await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				return base.ExecuteScalar();
			}
		}

		public virtual Task<IDataReader> ExecuteReaderAsync()
		{
			return ExecuteReaderAsync(CancellationToken.None);
		}

		public async virtual Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
		{
			this.context.currentReader?.BufferAll();
			var dbCommand = this.Inner as DbCommand;
			if (dbCommand != null)
			{
				try
				{
					return new MarsDataReader(this, (await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)));
				}
				catch (Exception)
				{
					throw;
				}
			}
			else
			{
				return new MarsDataReader(this, base.ExecuteReader());
			}
		}

		public virtual Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior)
		{
			return ExecuteReaderAsync(behavior, CancellationToken.None);
		}

		public async virtual Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			this.context.currentReader?.BufferAll();
			var dbCommand = this.Inner as DbCommand;
			if (dbCommand != null)
			{
				return new MarsDataReader(this, (await dbCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false)));
			}
			else
			{
				return new MarsDataReader(this, base.ExecuteReader(behavior));
			}
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using Shaolinq;
	using Shaolinq.Persistence;
	using Shaolinq.Persistence.Linq.Expressions;

	public abstract partial class SqlTransactionalCommandsContext
	{
		public virtual Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public virtual async Task CommitAsync(CancellationToken cancellationToken)
		{
			try
			{
				if (this.dbTransaction != null)
				{
					await DbTransactionExtensions.CommitAsync(this.dbTransaction, cancellationToken).ConfigureAwait(false);
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
				this.CloseConnection();
			}
		}

		public virtual Task RollbackAsync()
		{
			return RollbackAsync(CancellationToken.None);
		}

		public virtual async Task RollbackAsync(CancellationToken cancellationToken)
		{
			try
			{
				if (this.dbTransaction != null)
				{
					await DbTransactionExtensions.RollbackAsync(this.dbTransaction, cancellationToken).ConfigureAwait(false);
					this.dbTransaction = null;
				}
			}
			finally
			{
				this.CloseConnection();
			}
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Linq;
	using System.Threading;
	using System.Reflection;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
	using Platform;
	using Shaolinq.Persistence;
	using Shaolinq.TypeBuilding;
	using Shaolinq.Persistence.Linq;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;
	using global::Shaolinq.TypeBuilding;
	using global::Shaolinq.Persistence.Linq;

	public static partial class QueryableExtensions
	{
		public static Task<bool> AnyAsync<T>(this IQueryable<T> source)
		{
			return AnyAsync(source, CancellationToken.None);
		}

		public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Any<T>(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<bool>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return AnyAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Any<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<bool>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return AllAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.All<T>(default (IQueryable<T>), default (Expression<Func<T, bool>>))), source.Expression, Expression.Quote(predicate));
			return await SqlQueryProviderExtensions.ExecuteAsync<bool>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> FirstAsync<T>(this IQueryable<T> source)
		{
			return FirstAsync(source, CancellationToken.None);
		}

		public static async Task<T> FirstAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.First<T>(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> FirstAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return FirstAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<T> FirstAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.First<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source)
		{
			return FirstOrDefaultAsync(source, CancellationToken.None);
		}

		public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.FirstOrDefault<T>(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return FirstOrDefaultAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.FirstOrDefault<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> SingleAsync<T>(this IQueryable<T> source)
		{
			return SingleAsync(source, CancellationToken.None);
		}

		public static async Task<T> SingleAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Single<T>(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> SingleAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return SingleAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<T> SingleAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Single<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source)
		{
			return SingleOrDefaultAsync(source, CancellationToken.None);
		}

		public static async Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.SingleOrDefault<T>(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return SingleOrDefaultAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.SingleOrDefault<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> DeleteAsync<T>(this IQueryable<T> source)where T : DataAccessObject
		{
			return DeleteAsync(source, CancellationToken.None);
		}

		public static async Task<int> DeleteAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)where T : DataAccessObject
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.Delete<T>(default (IQueryable<T>))), source.Expression);
			await ((SqlQueryProvider)source.Provider).DataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> DeleteAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)where T : DataAccessObject
		{
			return DeleteAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<int> DeleteAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)where T : DataAccessObject
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => QueryableExtensions.Delete<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			await ((SqlQueryProvider)source.Provider).DataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> CountAsync<T>(this IQueryable<T> source)
		{
			return CountAsync(source, CancellationToken.None);
		}

		public static async Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Count(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return CountAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Count<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> LongCountAsync<T>(this IQueryable<T> source)
		{
			return LongCountAsync(source, CancellationToken.None);
		}

		public static async Task<long> LongCountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.LongCount(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> LongCountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return LongCountAsync(source, predicate, CancellationToken.None);
		}

		public static async Task<long> LongCountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.LongCount<T>(default (IQueryable<T>))), Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeof (T)), source.Expression, Expression.Quote(predicate)));
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> MinAsync<T>(this IQueryable<T> source)
		{
			return MinAsync(source, CancellationToken.None);
		}

		public static async Task<T> MinAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<T> MaxAsync<T>(this IQueryable<T> source)
		{
			return MaxAsync(source, CancellationToken.None);
		}

		public static async Task<T> MaxAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default (IQueryable<T>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<T>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<U> MinAsync<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			return MinAsync(source, selector, CancellationToken.None);
		}

		public static async Task<U> MinAsync<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Min(default (IQueryable<T>), c => default (U))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<U>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<U> MaxAsync<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector)
		{
			return MaxAsync(source, selector, CancellationToken.None);
		}

		public static async Task<U> MaxAsync<T, U>(this IQueryable<T> source, Expression<Func<T, U>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Max(default (IQueryable<T>), c => default (U))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<U>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> SumAsync(this IQueryable<int> source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<int>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int ? > SumAsync(this IQueryable<int ? > source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<int ? > SumAsync(this IQueryable<int ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<int ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<int ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> SumAsync(this IQueryable<long> source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<long>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long ? > SumAsync(this IQueryable<long ? > source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<long ? > SumAsync(this IQueryable<long ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<long ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<long ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float> SumAsync(this IQueryable<float> source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<float>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<float>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float ? > SumAsync(this IQueryable<float ? > source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<float ? > SumAsync(this IQueryable<float ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<float ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<float ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double> SumAsync(this IQueryable<double> source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<double>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<double>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double ? > SumAsync(this IQueryable<double ? > source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<double ? > SumAsync(this IQueryable<double ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<double ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<double ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<decimal>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<decimal>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<decimal ? > SumAsync(this IQueryable<decimal ? > source)
		{
			return SumAsync(source, CancellationToken.None);
		}

		public static async Task<decimal ? > SumAsync(this IQueryable<decimal ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<decimal ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<decimal ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<int> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (int))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int ? >> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<int ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (int ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<int ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<long> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, long>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (long))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, long ? >> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<long ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, long ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (long ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<long ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<float> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, float>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (float))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<float>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, float ? >> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<float ? > SumAsync<T>(this IQueryable<T> source, Expression<Func<T, float ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (float ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<float ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<double> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, double>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (double))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<double>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double ? > SumAsync<T>(this IQueryable<double ? > source, Expression<Func<T, double ? >> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<double ? > SumAsync<T>(this IQueryable<double ? > source, Expression<Func<T, double ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (double ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<double ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<decimal> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, decimal>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<decimal> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (decimal))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<decimal>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<decimal ? > SumAsync<T>(this IQueryable<decimal ? > source, Expression<Func<T, decimal ? >> selector)
		{
			return SumAsync(source, selector, CancellationToken.None);
		}

		public static async Task<decimal ? > SumAsync<T>(this IQueryable<decimal ? > source, Expression<Func<T, decimal ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Sum(default (IQueryable<T>), c => default (decimal ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<decimal ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> AverageAsync(this IQueryable<int> source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<int> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<int>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int ? > AverageAsync(this IQueryable<int ? > source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<int ? > AverageAsync(this IQueryable<int ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<int ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<int ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> AverageAsync(this IQueryable<long> source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<long> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<long>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long ? > AverageAsync(this IQueryable<long ? > source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<long ? > AverageAsync(this IQueryable<long ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<long ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<long ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<float>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<float>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float ? > AverageAsync(this IQueryable<float ? > source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<float ? > AverageAsync(this IQueryable<float ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<float ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<float ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<double>))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<double>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double ? > AverageAsync(this IQueryable<double ? > source)
		{
			return AverageAsync(source, CancellationToken.None);
		}

		public static async Task<double ? > AverageAsync(this IQueryable<double ? > source, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<double ? >))), source.Expression);
			return await SqlQueryProviderExtensions.ExecuteAsync<double ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<int> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (int))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<int>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<int ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int ? >> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<int ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (int ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<int ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, long>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<long> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, long>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (long))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<long>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<long ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, long ? >> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<long ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, long ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (long ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<long ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, float>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<float> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, float>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (float))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<float>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<float ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, float ? >> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<float ? > AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, float ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (float ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<float ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, double>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<double> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, double>> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (double))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<double>(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}

		public static Task<double ? > AverageAsync<T>(this IQueryable<double ? > source, Expression<Func<T, double ? >> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None);
		}

		public static async Task<double ? > AverageAsync<T>(this IQueryable<double ? > source, Expression<Func<T, double ? >> selector, CancellationToken cancellationToken)
		{
			Expression expression = Expression.Call(TypeUtils.GetMethod(() => Queryable.Average(default (IQueryable<T>), c => default (double ? ))), source.Expression, Expression.Quote(selector));
			return await SqlQueryProviderExtensions.ExecuteAsync<double ? >(((IQueryProvider)source.Provider), expression, cancellationToken).ConfigureAwait(false);
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Linq;
	using System.Threading;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using System.Collections.Generic;
	using Platform;
	using Shaolinq.Persistence;
	using Shaolinq.TypeBuilding;
	using Shaolinq.Persistence.Linq.Expressions;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;
	using global::Shaolinq.TypeBuilding;
	using global::Shaolinq.Persistence.Linq.Expressions;

	/// <summary>
	/// Stores a cache of all objects that have been loaded or created within a context
	/// of a transaction.
	/// Code repetition and/or ugliness in this class is due to the need for this
	/// code to run FAST.
	/// </summary>
	public partial class DataAccessObjectDataContext
	{
		public virtual Task CommitAsync(TransactionContext transactionContext, bool forFlush)
		{
			return CommitAsync(transactionContext, forFlush, CancellationToken.None);
		}

		public virtual async Task CommitAsync(TransactionContext transactionContext, bool forFlush, CancellationToken cancellationToken)
		{
			var acquisitions = new HashSet<DatabaseTransactionContextAcquisition>();
			foreach (var cache in this.cachesByType)
			{
				cache.Value.AssertObjectsAreReadyForCommit();
			}

			try
			{
				try
				{
					this.isCommiting = true;
					await this.CommitNewAsync(acquisitions, transactionContext, cancellationToken).ConfigureAwait(false);
					await this.CommitUpdatedAsync(acquisitions, transactionContext, cancellationToken).ConfigureAwait(false);
					await this.CommitDeletedAsync(acquisitions, transactionContext, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					this.isCommiting = false;
				}

				foreach (var cache in this.cachesByType)
				{
					cache.Value.ProcessAfterCommit();
				}
			}
			finally
			{
				Exception oneException = null;
				foreach (var acquisition in acquisitions)
				{
					try
					{
						acquisition.Dispose();
					}
					catch (Exception e)
					{
						oneException = e;
					}
				}

				if (oneException != null)
				{
					throw oneException;
				}
			}
		}

		private static Task CommitDeletedAsync(SqlDatabaseContext sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			return CommitDeletedAsync(sqlDatabaseContext, cache, acquisitions, transactionContext, CancellationToken.None);
		}

		private static async Task CommitDeletedAsync(SqlDatabaseContext sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext, CancellationToken cancellationToken)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);
			acquisitions.Add(acquisition);
			await acquisition.SqlDatabaseCommandsContext.DeleteAsync(cache.Type, cache.GetDeletedObjects(), cancellationToken).ConfigureAwait(false);
		}

		private Task CommitDeletedAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			return CommitDeletedAsync(acquisitions, transactionContext, CancellationToken.None);
		}

		private async Task CommitDeletedAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext, CancellationToken cancellationToken)
		{
			foreach (var cache in this.cachesByType)
			{
				await CommitDeletedAsync(this.SqlDatabaseContext, cache.Value, acquisitions, transactionContext, cancellationToken).ConfigureAwait(false);
			}
		}

		private static Task CommitUpdatedAsync(SqlDatabaseContext sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			return CommitUpdatedAsync(sqlDatabaseContext, cache, acquisitions, transactionContext, CancellationToken.None);
		}

		private static async Task CommitUpdatedAsync(SqlDatabaseContext sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext, CancellationToken cancellationToken)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);
			acquisitions.Add(acquisition);
			await acquisition.SqlDatabaseCommandsContext.UpdateAsync(cache.Type, cache.GetObjectsById(), cancellationToken).ConfigureAwait(false);
			await acquisition.SqlDatabaseCommandsContext.UpdateAsync(cache.Type, cache.GetObjectsByPredicate(), cancellationToken).ConfigureAwait(false);
		}

		private Task CommitUpdatedAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			return CommitUpdatedAsync(acquisitions, transactionContext, CancellationToken.None);
		}

		private async Task CommitUpdatedAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext, CancellationToken cancellationToken)
		{
			foreach (var cache in this.cachesByType)
			{
				await CommitUpdatedAsync(this.SqlDatabaseContext, cache.Value, acquisitions, transactionContext, cancellationToken).ConfigureAwait(false);
			}
		}

		private static Task CommitNewPhase1Async(SqlDatabaseContext sqlDatabaseContext, HashSet<DatabaseTransactionContextAcquisition> acquisitions, IObjectsByIdCache cache, TransactionContext transactionContext, Dictionary<TypeAndTransactionalCommandsContext, InsertResults> insertResultsByType, Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>> fixups)
		{
			return CommitNewPhase1Async(sqlDatabaseContext, acquisitions, cache, transactionContext, insertResultsByType, fixups, CancellationToken.None);
		}

		private static async Task CommitNewPhase1Async(SqlDatabaseContext sqlDatabaseContext, HashSet<DatabaseTransactionContextAcquisition> acquisitions, IObjectsByIdCache cache, TransactionContext transactionContext, Dictionary<TypeAndTransactionalCommandsContext, InsertResults> insertResultsByType, Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>> fixups, CancellationToken cancellationToken)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);
			acquisitions.Add(acquisition);
			var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;
			var key = new TypeAndTransactionalCommandsContext(cache.Type, persistenceTransactionContext);
			var currentInsertResults = (await persistenceTransactionContext.InsertAsync(cache.Type, cache.GetNewObjects(), cancellationToken).ConfigureAwait(false));
			if (currentInsertResults.ToRetry.Count > 0)
			{
				insertResultsByType[key] = currentInsertResults;
			}

			if (currentInsertResults.ToFixUp.Count > 0)
			{
				fixups[key] = currentInsertResults.ToFixUp;
			}
		}

		private Task CommitNewAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			return CommitNewAsync(acquisitions, transactionContext, CancellationToken.None);
		}

		private async Task CommitNewAsync(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext, CancellationToken cancellationToken)
		{
			var fixups = new Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();
			foreach (var value in this.cachesByType.Values)
			{
				await CommitNewPhase1Async(this.SqlDatabaseContext, acquisitions, value, transactionContext, insertResultsByType, fixups, cancellationToken).ConfigureAwait(false);
			}

			var currentInsertResultsByType = insertResultsByType;
			var newInsertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();
			while (true)
			{
				var didRetry = false;
				// Perform the retry list
				foreach (var i in currentInsertResultsByType)
				{
					var type = i.Key.Type;
					var persistenceTransactionContext = i.Key.CommandsContext;
					var retryListForType = i.Value.ToRetry;
					if (retryListForType.Count == 0)
					{
						continue;
					}

					didRetry = true;
					newInsertResultsByType[new TypeAndTransactionalCommandsContext(type, persistenceTransactionContext)] = (await persistenceTransactionContext.InsertAsync(type, retryListForType, cancellationToken).ConfigureAwait(false));
				}

				if (!didRetry)
				{
					break;
				}

				MathUtils.Swap(ref currentInsertResultsByType, ref newInsertResultsByType);
				newInsertResultsByType.Clear();
			}

			// Perform fixups
			foreach (var i in fixups)
			{
				var type = i.Key.Type;
				var databaseTransactionContext = i.Key.CommandsContext;
				await databaseTransactionContext.UpdateAsync(type, i.Value, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}

namespace Shaolinq.Persistence
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Data.Common;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using Shaolinq;
	using Shaolinq.Persistence;
	using Shaolinq.Persistence.Linq;

	public abstract partial class SqlDatabaseContext
	{
		public virtual Task<IDbConnection> OpenConnectionAsync()
		{
			return OpenConnectionAsync(CancellationToken.None);
		}

		public virtual async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			if (this.dbProviderFactory == null)
			{
				this.dbProviderFactory = this.CreateDbProviderFactory();
			}

			var retval = this.dbProviderFactory.CreateConnection();
			retval.ConnectionString = this.ConnectionString;
			await retval.OpenAsync(cancellationToken).ConfigureAwait(false);
			return retval;
		}

		public virtual Task<IDbConnection> OpenServerConnectionAsync()
		{
			return OpenServerConnectionAsync(CancellationToken.None);
		}

		public virtual async Task<IDbConnection> OpenServerConnectionAsync(CancellationToken cancellationToken)
		{
			if (this.dbProviderFactory == null)
			{
				this.dbProviderFactory = this.CreateDbProviderFactory();
			}

			var retval = this.dbProviderFactory.CreateConnection();
			retval.ConnectionString = this.ServerConnectionString;
			await retval.OpenAsync(cancellationToken).ConfigureAwait(false);
			return retval;
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Linq;
	using System.Threading;
	using System.Reflection;
	using System.Diagnostics;
	using System.Configuration;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using System.Collections.Generic;
	using Platform;
	using Shaolinq.Analytics;
	using Shaolinq.Persistence;
	using Shaolinq.TypeBuilding;
	using Shaolinq.Persistence.Linq.Optimizers;
	using global::Shaolinq;
	using global::Shaolinq.Analytics;
	using global::Shaolinq.Persistence;
	using global::Shaolinq.TypeBuilding;
	using global::Shaolinq.Persistence.Linq.Optimizers;

	public partial class DataAccessModel
	{
		public virtual Task FlushAsync()
		{
			return FlushAsync(CancellationToken.None);
		}

		public virtual async Task FlushAsync(CancellationToken cancellationToken)
		{
			using (var acquisition = TransactionContext.Acquire(this, true))
			{
				var transactionContext = acquisition.TransactionContext;
				await transactionContext.GetCurrentDataContext().CommitAsync(transactionContext, true, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Transactions;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using Platform;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	public partial class TransactionContext
	{
		public Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public async Task CommitAsync(CancellationToken cancellationToken)
		{
			if (this.disposed)
			{
				return;
			}

			try
			{
				if (this.dataAccessObjectDataContext != null)
				{
					await this.dataAccessObjectDataContext.CommitAsync(this, false, cancellationToken).ConfigureAwait(false);
				}

				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					await commandsContext.CommitAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				this.commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));
				throw new DataAccessTransactionAbortedException(e);
			}
			finally
			{
				this.Dispose();
			}
		}

		internal Task RollbackAsync()
		{
			return RollbackAsync(CancellationToken.None);
		}

		internal async Task RollbackAsync(CancellationToken cancellationToken)
		{
			if (this.disposed)
			{
				return;
			}

			try
			{
				foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
				{
					ActionUtils.IgnoreExceptions(() => commandsContext.Rollback());
				}
			}
			finally
			{
				this.Dispose();
			}
		}
	}
}

namespace Shaolinq
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Transactions;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using Shaolinq.Persistence;
	using global::Shaolinq;
	using global::Shaolinq.Persistence;

	public static partial class TransactionScopeExtensions
	{
		public static Task SaveAsync(this TransactionScope scope)
		{
			return SaveAsync(scope, CancellationToken.None);
		}

		public static async Task SaveAsync(this TransactionScope scope, CancellationToken cancellationToken)
		{
			if (DataAccessTransaction.Current == null)
			{
				return;
			}

			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					await dataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
				}
			}
		}

		public static Task SaveAsync(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			return SaveAsync(scope, dataAccessModel, CancellationToken.None);
		}

		public static async Task SaveAsync(this TransactionScope scope, DataAccessModel dataAccessModel, CancellationToken cancellationToken)
		{
			if (!dataAccessModel.IsDisposed)
			{
				await dataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		public static Task FlushAsync(this TransactionScope scope)
		{
			return FlushAsync(scope, CancellationToken.None);
		}

		public static async Task FlushAsync(this TransactionScope scope, CancellationToken cancellationToken)
		{
			await scope.SaveAsync(cancellationToken).ConfigureAwait(false);
		}

		public static Task FlushAsync(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			return FlushAsync(scope, dataAccessModel, CancellationToken.None);
		}

		public static async Task FlushAsync(this TransactionScope scope, DataAccessModel dataAccessModel, CancellationToken cancellationToken)
		{
			await scope.SaveAsync(dataAccessModel, cancellationToken).ConfigureAwait(false);
		}
	}
}