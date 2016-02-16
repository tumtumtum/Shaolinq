#pragma warning disable
using System;
using Shaolinq.Persistence;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Transactions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
#pragma warning disable
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
    public partial class DataAccessScope
    {
        public Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task FlushAsync(DataAccessModel dataAccessModel)
        {
            return FlushAsync(dataAccessModel, CancellationToken.None);
        }

        public async Task FlushAsync(DataAccessModel dataAccessModel, CancellationToken cancellationToken)
        {
            await SaveAsync(dataAccessModel, cancellationToken).ConfigureAwait(false);
        }

        public Task SaveAsync()
        {
            return SaveAsync(CancellationToken.None);
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
            {
                if (!dataAccessModel.IsDisposed)
                {
                    await dataAccessModel.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task SaveAsync(DataAccessModel dataAccessModel)
        {
            return SaveAsync(dataAccessModel, CancellationToken.None);
        }

        public async Task SaveAsync(DataAccessModel dataAccessModel, CancellationToken cancellationToken)
        {
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
            this.complete = true;
            if (this.transaction == null)
            {
                return;
            }

            if (this.transaction.HasSystemTransaction)
            {
                return;
            }

            if (this.transaction != DataAccessTransaction.Current)
            {
                throw new InvalidOperationException($"Cannot dispose {this.GetType().Name} within another Async/Call context");
            }

            await this.transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.transaction.Dispose();
        }

        public Task FailAsync()
        {
            return FailAsync(CancellationToken.None);
        }

        public async Task FailAsync(CancellationToken cancellationToken)
        {
            this.complete = false;
            if (this.transaction != null)
            {
                await this.transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                this.transaction.Dispose();
            }
        }
    }

    public partial class DataAccessTransaction
    {
        public Task CommitAsync()
        {
            return CommitAsync(CancellationToken.None);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            this.isfinishing = true;
            foreach (var transactionContext in this.dataAccessModelsByTransactionContext.Values)
            {
                await transactionContext.CommitAsync(cancellationToken).ConfigureAwait(false);
                transactionContext.Dispose();
            }
        }

        public Task RollbackAsync()
        {
            return RollbackAsync(CancellationToken.None);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            this.isfinishing = true;
            foreach (var transactionContext in this.dataAccessModelsByTransactionContext.Values)
            {
                await transactionContext.RollbackAsync(cancellationToken).ConfigureAwait(false);
                transactionContext.Dispose();
            }
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
                var result = (await this.enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false));
            if (!result)
            {
                this.Current = this.specifiedValue ?? default (T);
                this.state = 9;
                return true;
            }
            else
            {
                this.state = 1;
                this.Current = this.specifiedValue;
                return true;
            }

            state1:
                result = (await this.enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false));
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
                var result = (await this.enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false));
            if (!result)
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
                result = (await this.enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false));
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

    public static partial class EnumerableExtensions
    {
        public static Task<T> SingleOrSpecifiedValueIfFirstIsDefaultValueAsync<T>(this IEnumerable<T> enumerable, T specifiedValue)
        {
            return SingleOrSpecifiedValueIfFirstIsDefaultValueAsync(enumerable, specifiedValue, CancellationToken.None);
        }

        public static async Task<T> SingleOrSpecifiedValueIfFirstIsDefaultValueAsync<T>(this IEnumerable<T> enumerable, T specifiedValue, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    return await new T[0].SingleAsync(cancellationToken).ConfigureAwait(false);
                }

                var result = enumerator.Current;
                if (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    return await new T[2].SingleAsync(cancellationToken).ConfigureAwait(false);
                }

                if (object.Equals(result, default (T)))
                {
                    return specifiedValue;
                }

                return result;
            }
        }

        public static Task<T> SingleAsync<T>(this IEnumerable<T> enumerable)
        {
            return SingleAsync(enumerable, CancellationToken.None);
        }

        public static async Task<T> SingleAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    return await new T[0].SingleAsync(cancellationToken).ConfigureAwait(false);
                }

                var result = enumerator.Current;
                if (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    return await new T[2].SingleAsync(cancellationToken).ConfigureAwait(false);
                }

                return result;
            }
        }

        public static Task<T> SingleOrDefaultAsync<T>(this IEnumerable<T> enumerable)
        {
            return SingleOrDefaultAsync(enumerable, CancellationToken.None);
        }

        public static async Task<T> SingleOrDefaultAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    return default (T);
                }

                var result = enumerator.Current;
                if (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    return await new T[2].SingleAsync(cancellationToken).ConfigureAwait(false);
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
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    return Enumerable.Empty<T>().First();
                }

                return enumerator.Current;
            }
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> enumerable)
        {
            return FirstOrDefaultAsync(enumerable, CancellationToken.None);
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    return Enumerable.Empty<T>().First();
                }

                return enumerator.Current;
            }
        }

        public static Task<T> SingleOrExceptionIfFirstIsNullAsync<T>(this IEnumerable<T? > enumerable)where T : struct
        {
            return SingleOrExceptionIfFirstIsNullAsync(enumerable, CancellationToken.None);
        }

        public static async Task<T> SingleOrExceptionIfFirstIsNullAsync<T>(this IEnumerable<T? > enumerable, CancellationToken cancellationToken)where T : struct
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                if (!(await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false)) || enumerator.Current == null)
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }

                return enumerator.Current.Value;
            }
        }

        public static Task WithEachAsync<T>(this IEnumerable<T> enumerable, Action<T> value)
        {
            return WithEachAsync(enumerable, value, CancellationToken.None);
        }

        public static async Task WithEachAsync<T>(this IEnumerable<T> enumerable, Action<T> value, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                while (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    value(enumerator.Current);
                }
            }
        }

        public static Task WithEachAsync<T>(this IEnumerable<T> enumerable, Func<T, bool> value)
        {
            return WithEachAsync(enumerable, value, CancellationToken.None);
        }

        public static async Task WithEachAsync<T>(this IEnumerable<T> enumerable, Func<T, bool> value, CancellationToken cancellationToken)
        {
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                while (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!value(enumerator.Current))
                    {
                        break;
                    }
                }
            }
        }

        public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> enumerable)
        {
            return ToListAsync(enumerable, CancellationToken.None);
        }

        public static async Task<List<T>> ToListAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            if (enumerable == null)
            {
                return null;
            }

            var result = enumerable as List<T>;
            if (result != null)
            {
                return result;
            }

            result = new List<T>();
            using (var enumerator = (await enumerable.GetEnumeratorExAsync().ConfigureAwait(false)))
            {
                while (await enumerator.MoveNextExAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(enumerator.Current);
                }
            }

            return result;
        }

        public static Task<ReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IEnumerable<T> enumerable)
        {
            return ToReadOnlyCollectionAsync(enumerable, CancellationToken.None);
        }

        public static async Task<ReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            if (enumerable == null)
            {
                return null;
            }

            var readOnlyCollection = enumerable as ReadOnlyCollection<T>;
            if (readOnlyCollection != null)
            {
                return readOnlyCollection;
            }

            var list = enumerable as List<T>;
            if (list != null)
            {
                return new ReadOnlyCollection<T>(list);
            }

            return new ReadOnlyCollection<T>((await enumerable.ToListAsync(cancellationToken).ConfigureAwait(false)));
        }
    }

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

    public partial class DataAccessModel
    {
        public virtual Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
        }

        public virtual async Task FlushAsync(CancellationToken cancellationToken)
        {
            var transactionContext = this.GetCurrentContext(true);
            using (var context = transactionContext.AcquireVersionContext())
            {
                await this.GetCurrentDataContext(true).CommitAsync(transactionContext, true, cancellationToken).ConfigureAwait(false);
            }
        }
    }

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
                throw new ObjectDisposedException(nameof(TransactionContext));
            }

            try
            {
                // ReSharper disable once UseNullPropagation
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
                commandsContextsBySqlDatabaseContexts.Values.ForEach(c => ActionUtils.IgnoreExceptions(c.Rollback));
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
                throw new ObjectDisposedException(nameof(TransactionContext));
            }

            try
            {
                foreach (var commandsContext in this.commandsContextsBySqlDatabaseContexts.Values)
                {
                    await commandsContext.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                this.Dispose();
            }
        }
    }

    public static partial class TransactionScopeExtensions
    {
        public static Task SaveAsync(this TransactionScope scope)
        {
            return SaveAsync(scope, CancellationToken.None);
        }

        public static async Task SaveAsync(this TransactionScope scope, CancellationToken cancellationToken)
        {
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

namespace Shaolinq.Persistence
{
    public static partial class DbCommandExtensions
    {
        public static Task<IDataReader> ExecuteReaderExAsync(this IDbCommand command)
        {
            return ExecuteReaderExAsync(command, CancellationToken.None);
        }

        public static async Task<IDataReader> ExecuteReaderExAsync(this IDbCommand command, CancellationToken cancellationToken)
        {
            var marsDbCommand = command as MarsDbCommand;
            if (marsDbCommand != null)
            {
                return await marsDbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            }

            var dbCommand = command as DbCommand;
            if (dbCommand != null)
            {
                return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            }

            return command.ExecuteReader();
        }

        public static Task<int> ExecuteNonQueryExAsync(this IDbCommand command)
        {
            return ExecuteNonQueryExAsync(command, CancellationToken.None);
        }

        public static async Task<int> ExecuteNonQueryExAsync(this IDbCommand command, CancellationToken cancellationToken)
        {
            var marsDbCommand = command as MarsDbCommand;
            if (marsDbCommand != null)
            {
                return await marsDbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            var dbCommand = command as DbCommand;
            if (dbCommand != null)
            {
                return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            return command.ExecuteNonQuery();
        }
    }

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

    public partial class DefaultSqlTransactionalCommandsContext
    {
        public override Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<Tuple<Type, object>> parameters)
        {
            return ExecuteReaderAsync(sql, parameters, CancellationToken.None);
        }

        public override async Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<Tuple<Type, object>> parameters, CancellationToken cancellationToken)
        {
            using (var command = this.CreateCommand())
            {
                foreach (var value in parameters)
                {
                    this.AddParameter(command, value.Item1, value.Item2);
                }

                command.CommandText = sql;
                Logger.Debug(() => this.FormatCommand(command));
                try
                {
                    return await command.ExecuteReaderExAsync(cancellationToken).ConfigureAwait(false);
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
            var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
            foreach (var dataAccessObject in dataAccessObjects)
            {
                var objectState = dataAccessObject.GetAdvanced().ObjectState;
                if ((objectState & (ObjectState.Changed | ObjectState.ServerSidePropertiesHydrated)) == 0)
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

                    Logger.Debug(() => this.FormatCommand(command));
                    int result;
                    try
                    {
                        result = (await command.ExecuteNonQueryExAsync(cancellationToken).ConfigureAwait(false));
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

                    dataAccessObject.ToObjectInternal().ResetModified();
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
                switch (objectState & ObjectState.NewChanged)
                {
                    case ObjectState.Unchanged:
                        continue;
                    case ObjectState.New:
                    case ObjectState.NewChanged:
                        break;
                    case ObjectState.Changed:
                        throw new NotSupportedException("Changed state not supported");
                }

                var primaryKeyIsComplete = (objectState & ObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) == 0;
                var deferrableOrNotReferencingNewObject = (this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.Deferrability) || ((objectState & ObjectState.ReferencesNewObject) == 0));
                var objectReadyToBeCommited = primaryKeyIsComplete && deferrableOrNotReferencingNewObject;
                if (objectReadyToBeCommited)
                {
                    var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
                    using (var command = this.BuildInsertCommand(typeDescriptor, dataAccessObject))
                    {
                        Logger.Debug(() => this.FormatCommand(command));
                        try
                        {
                            var reader = (await command.ExecuteReaderExAsync(cancellationToken).ConfigureAwait(false));
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
                                        await this.UpdateAsync(dataAccessObject.GetType(), new[]{dataAccessObject}, cancellationToken).ConfigureAwait(false);
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

                        if ((objectState & ObjectState.ReferencesNewObjectWithServerSideProperties) == ObjectState.ReferencesNewObjectWithServerSideProperties)
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
                    this.AddParameter(command, value.Item1, value.Item2);
                }

                Logger.Debug(() => this.FormatCommand(command));
                try
                {
                    await command.ExecuteNonQueryExAsync(cancellationToken).ConfigureAwait(false);
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
            var expression = (Expression)Expression.Call(GetDeleteMethod(typeDescriptor.Type), Expression.Constant(null, typeDescriptor.Type), condition);
            expression = Evaluator.PartialEval(expression);
            expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
            expression = SqlObjectOperandComparisonExpander.Expand(expression);
            expression = SqlQueryProvider.Optimize(this.DataAccessModel, expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());
            await this.DeleteAsync((SqlDeleteExpression)expression, cancellationToken).ConfigureAwait(false);
        }
    }

    public partial class MarsDbCommand
    {
        public Task<int> ExecuteNonQueryAsync()
        {
            return ExecuteNonQueryAsync(CancellationToken.None);
        }

        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
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

        public Task<object> ExecuteScalarAsync()
        {
            return ExecuteScalarAsync(CancellationToken.None);
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
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

        public Task<IDataReader> ExecuteReaderAsync()
        {
            return ExecuteReaderAsync(CancellationToken.None);
        }

        public async Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            this.context.currentReader?.BufferAll();
            var dbCommand = this.Inner as DbCommand;
            if (dbCommand != null)
            {
                return new MarsDataReader(this, (await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)));
            }
            else
            {
                return new MarsDataReader(this, base.ExecuteReader());
            }
        }

        public Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        {
            return ExecuteReaderAsync(behavior, CancellationToken.None);
        }

        public async Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
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
                    await this.dbTransaction.CommitExAsync(cancellationToken).ConfigureAwait(false);
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
                    await this.dbTransaction.RollbackExAsync(cancellationToken).ConfigureAwait(false);
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

namespace Shaolinq.Persistence.Linq
{
    internal partial class AsyncEnumerator<T, U>
    {
        public virtual Task<bool> MoveNextAsync()
        {
            return MoveNextAsync(CancellationToken.None);
        }

        public virtual async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            if (this.dataReader == null)
            {
                this.dataReader = (await this.acquisition.SqlDatabaseCommandsContext.ExecuteReaderAsync(this.objectProjector.formatResult.CommandText, this.objectProjector.formatResult.ParameterValues, cancellationToken).ConfigureAwait(false));
            }

            while (true)
            {
                T result;
                if (this.eof)
                {
                    return false;
                }

                if (!(await this.dataReader.ReadExAsync(cancellationToken).ConfigureAwait(false)))
                {
                    if (this.objectProjector.ProcessLastMoveNext(ref this.context, out result))
                    {
                        this.eof = true;
                        this.Current = result;
                        return true;
                    }

                    return false;
                }

                T value = this.objectProjector.objectReader(this.objectProjector, this.dataReader, this.versionContext.Version, this.objectProjector.placeholderValues);
                if (this.objectProjector.ProcessMoveNext(value, ref this.context, out result))
                {
                    this.Current = value;
                    return true;
                }
            }
        }
    }
}