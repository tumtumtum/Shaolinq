// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Transactions;

namespace Shaolinq
{
	public static class TransactionScopeFactory
	{
		public static TransactionScope CreateReadCommitted(TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, TimeSpan? timeout = null)
		{
			return CreateReadCommitted(transactionScopeOption, timeout, TransactionScopeAsyncFlowOption.Enabled);
		}

		public static TransactionScope CreateReadCommitted(TransactionScopeOption transactionScopeOption, TimeSpan? timeout, TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption)
		{
			var transactionOptions = new TransactionOptions
			{
				IsolationLevel = IsolationLevel.ReadCommitted,
			};
            
			if (transactionScopeOption == TransactionScopeOption.Required)
			{

                var currentTransaction = Transaction.Current;
				if (currentTransaction != null && (currentTransaction.IsolationLevel == IsolationLevel.Serializable || currentTransaction.IsolationLevel == IsolationLevel.RepeatableRead))
				{
					transactionOptions.IsolationLevel = currentTransaction.IsolationLevel;
				}
			}

			if (timeout.HasValue)
			{
				transactionOptions.Timeout = timeout.Value;
			}

			return new TransactionScope(transactionScopeOption, transactionOptions, transactionScopeAsyncFlowOption);
		}

		public static TransactionScope CreateRepeatableRead(TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, TimeSpan? timeout = null)
		{
			return CreateRepeatableRead(transactionScopeOption, timeout, TransactionScopeAsyncFlowOption.Enabled);
		}

		public static TransactionScope CreateRepeatableRead(TransactionScopeOption transactionScopeOption, TimeSpan? timeout, TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption)
		{
			var transactionOptions = new TransactionOptions
			{
				IsolationLevel = IsolationLevel.RepeatableRead,
			};
            
			if (transactionScopeOption == TransactionScopeOption.Required)
			{
				var currentTransaction = Transaction.Current;

				if (currentTransaction != null && currentTransaction.IsolationLevel == IsolationLevel.Serializable)
				{
					transactionOptions.IsolationLevel = currentTransaction.IsolationLevel;
				}
			}

			if (timeout.HasValue)
			{
				transactionOptions.Timeout = timeout.Value;
			}

			return new TransactionScope(transactionScopeOption, transactionOptions, transactionScopeAsyncFlowOption);
		}

		public static TransactionScope CreateSerializable(TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, TimeSpan? timeout = null)
		{
			return CreateSerializable(transactionScopeOption, timeout, TransactionScopeAsyncFlowOption.Enabled);
		}
			
		public static TransactionScope CreateSerializable(TransactionScopeOption transactionScopeOption, TimeSpan? timeout, TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption)
		{
			var transactionOptions = new TransactionOptions
			{
				IsolationLevel = IsolationLevel.Serializable,
			};

			if (timeout.HasValue)
			{
				transactionOptions.Timeout = timeout.Value;
			}

			return new TransactionScope(transactionScopeOption, transactionOptions, transactionScopeAsyncFlowOption);
		}
	}
}