// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Shaolinq.Tests
{
	public class TransactionScopeAdapter
		: IDisposable
	{
		private readonly DataAccessScope dataAccessScope;
		private readonly TransactionScope transactionScope;

		public TransactionScopeAdapter(DataAccessScope dataAccessScope)
		{
			this.dataAccessScope = dataAccessScope;
		}

		public TransactionScopeAdapter(TransactionScope transactionScope)
		{
			this.transactionScope = transactionScope;
		}

		public void Complete()
		{
			this.dataAccessScope?.Complete();
			this.transactionScope?.Complete();
		}

		public async Task CompleteAsync()
		{
			await this.dataAccessScope?.CompleteAsync();
			this.transactionScope?.Complete();
		}

		public void Flush()
		{
			this.dataAccessScope?.Flush();
			this.transactionScope?.Flush();
		}

		public void Flush(DataAccessModel model)
		{
			this.dataAccessScope?.Flush(model);
			this.transactionScope?.Flush(model);
		}

		public async Task FlushAsync()
		{
			await this.dataAccessScope?.FlushAsync();
			await this.transactionScope?.FlushAsync();
		}
        
		public void Dispose()
		{
			this.dataAccessScope?.Dispose();
			this.transactionScope?.Dispose();
		}	
	}
}