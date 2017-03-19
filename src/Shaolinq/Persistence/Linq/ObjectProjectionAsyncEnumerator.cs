// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	internal partial class ObjectProjectionAsyncEnumerator<T, U, C>
		: IAsyncEnumerator<T>
		where U : T
		where C : class
	{
		private int state;
		private bool disposed;
		private C context;
		private IDataReader dataReader;
		private readonly ObjectProjector<T, U, C> objectProjector;
		private readonly TransactionContext.TransactionExecutionContext transactionExecutionContextAcquisition;

		public ObjectProjectionAsyncEnumerator(ObjectProjector<T, U, C> objectProjector)
		{
			this.objectProjector = objectProjector;

			try
			{
				this.transactionExecutionContextAcquisition = TransactionContext
					.Acquire(this.objectProjector.DataAccessModel, false);
			}
			catch
			{
				this.Dispose();

				throw;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(nameof(ObjectProjectionAsyncEnumerator<T, U, C>));
			}

			this.disposed = true;

			// ReSharper disable EmptyGeneralCatchClause
			try { this.dataReader?.Dispose(); } catch { }
			try { this.transactionExecutionContextAcquisition?.Dispose(); } catch { }
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

			this.state = 1;
			var commandsContext = this.transactionExecutionContextAcquisition.TransactionContext.GetSqlTransactionalCommandsContext();
			this.dataReader = commandsContext.ExecuteReader(this.objectProjector.formatResult.CommandText, this.objectProjector.formatResult.ParameterValues);
			this.context = this.objectProjector.CreateEnumerationContext(this.dataReader, this.transactionExecutionContextAcquisition.Version);

state1:
			T result;

			if (this.dataReader.ReadEx())
			{
				T value = this.objectProjector.objectReader(this.objectProjector, this.dataReader, this.transactionExecutionContextAcquisition.Version, this.objectProjector.placeholderValues, o => this.objectProjector.ProcessDataAccessObject(o, ref this.context));

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