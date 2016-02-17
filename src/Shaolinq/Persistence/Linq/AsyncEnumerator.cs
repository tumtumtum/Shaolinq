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
        private int state;
        private IDataReader dataReader;
        private readonly ObjectProjector<T, U> objectProjector;
        private readonly DatabaseTransactionContextAcquisition acquisition;
        private readonly TransactionContext.TransactionContextExecutionVersionContext versionContext;
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
            this.dataReader = this.acquisition.SqlDatabaseCommandsContext.ExecuteReader(this.objectProjector.formatResult.CommandText, this.objectProjector.formatResult.ParameterValues);
            
state1:
            T result;

            if (this.dataReader.ReadEx())
            {
                T value = this.objectProjector.objectReader(this.objectProjector, this.dataReader, this.versionContext.Version, this.objectProjector.placeholderValues);

                if (this.objectProjector.ProcessMoveNext(value, ref this.context, out result))
                {
                    this.Current = result;

                    return true;
                }

                goto state1;
            }
            
            this.state = 9;

            if (this.objectProjector.ProcessLastMoveNext(ref this.context, out result))
            {
                this.Current = result;

                return true;
            }

state9:

            return false;
        }
    }
}