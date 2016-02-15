// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
    internal partial class AsyncEnumerator<T, U>
        : IAsyncEnumerator<T>, IEnumerator<T>
        where U : T
    {
        private IDataReader dataReader;
        private readonly ObjectProjector<T, U> objectProjector;
        private readonly DatabaseTransactionContextAcquisition acquisition;
        private readonly TransactionContext.TransactionContextExecutionVersionContext versionContext;
        private bool eof;
        private object context;

        public AsyncEnumerator(ObjectProjector<T, U> objectProjector)
        {
            this.objectProjector = objectProjector;

            var transactionContext = this.objectProjector.DataAccessModel.GetCurrentContext(false);

            this.versionContext = transactionContext.AcquireVersionContext();
            this.acquisition = transactionContext.AcquirePersistenceTransactionContext(this.objectProjector.SqlDatabaseContext);
            this.context = objectProjector.CreateEnumerationContext();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // ReSharper disable EmptyGeneralCatchClause
            try { this.dataReader.Dispose(); } catch { }
            try { this.acquisition.Dispose(); } catch { }
            try { this.versionContext.Dispose(); } catch { }
            // ReSharper restore EmptyGeneralCatchClause
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current => this.Current;
        public virtual T Current { get; private set; }

        [RewriteAsync]
        public virtual bool MoveNext()
        {
            if (this.dataReader == null)
            {
                this.dataReader = this.acquisition.SqlDatabaseCommandsContext.ExecuteReader(this.objectProjector.formatResult.CommandText, this.objectProjector.formatResult.ParameterValues);
            }

            while (true)
            {
                T result;

                if (this.eof)
                {
                    return false;
                }

                if (!this.dataReader.ReadEx())
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