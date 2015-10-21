// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Transactions;
using log4net;
using Platform;

namespace Shaolinq
{
    public static class TransactionScopeFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TransactionScopeFactory).Name);

        public static TransactionScope CreateReadCommitted(
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            TimeSpan? timeout = null)
            //System.Transactions.TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) // .NET 4.5.1 feature
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
            };

            // Upgrade isolation level if necessary
            if (transactionScopeOption == TransactionScopeOption.Required)
            {
                var currentTransaction = Transaction.Current;
                if (currentTransaction != null &&
                    (currentTransaction.IsolationLevel == IsolationLevel.Serializable || currentTransaction.IsolationLevel == IsolationLevel.RepeatableRead))
                {
                    transactionOptions.IsolationLevel = currentTransaction.IsolationLevel;
                }
            }

            if (timeout.HasValue)
            {
                transactionOptions.Timeout = timeout.Value;
            }

            return new TransactionScope(transactionScopeOption, transactionOptions/*, transactionScopeAsyncFlowOption*/);
        }

        public static TransactionScope CreateRepeatableRead(
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            TimeSpan? timeout = null)
            //TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) // .NET 4.5.1 feature
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.RepeatableRead,
            };

            // Upgrade isolation level if necessary
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

            return new TransactionScope(transactionScopeOption, transactionOptions/*, transactionScopeAsyncFlowOption*/);
        }

        public static TransactionScope CreateSerializable(
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            TimeSpan? timeout = null)
            //TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) // .NET 4.5.1 feature
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
            };

            if (timeout.HasValue)
            {
                transactionOptions.Timeout = timeout.Value;
            }

            return new TransactionScope(transactionScopeOption, transactionOptions/*, transactionScopeAsyncFlowOption*/);
        }

        public static void InvokeWithConcurrencyRetryAllowNested(
            Action<TransactionScope> action,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            int maxRetries = 3,
            TimeSpan? timeout = null)
            //TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) // .NET 4.5.1 feature
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // If this is an inner transaction, the outer transaction is responsible for the retry logic
            if (transactionScopeOption == TransactionScopeOption.Required && Transaction.Current != null)
            {
                using (var scope = CreateSerializable(transactionScopeOption, timeout/*, transactionScopeAsyncFlowOption*/))
                {
                    action(scope);
                }
            }
            else
            {
                var attempt = 0;

                ActionUtils.RetryAction(
                    () =>
                    {
                        attempt++;
                        using (var scope = CreateSerializable(transactionScopeOption, timeout/*, transactionScopeAsyncFlowOption*/))
                        {
                            action(scope);
                        }
                    },
                    TimeSpan.MaxValue,
                    ex =>
                    {
                        if (ex is TransactionAbortedException &&
                            ex.InnerException is ConcurrencyException)
                        {
                            var msg = string.Format("Transaction concurrency fault occured. Attempt: {0}/{1}", attempt, maxRetries);
                            Log.Warn(msg, ex);

                            if (attempt < maxRetries)
                            {
                                return true;
                            }
                        }

                        return false;
                    });
            }
        }

        public static void InvokeWithConcurrencyRetry(
            Action<TransactionScope> action,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            int maxRetries = 3,
            TimeSpan? timeout = null)
            //TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Suppress) // .NET 4.5.1 feature
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (transactionScopeOption == TransactionScopeOption.Required && Transaction.Current != null)
            {
                throw new TransactionException("Error, ambient transaction already exists. Retry must occur on outer or new transaction scope");
            }

            var attempt = 0;

            ActionUtils.RetryAction(
                () =>
                {
                    attempt++;
                    using (var scope = CreateSerializable(transactionScopeOption, timeout/*, transactionScopeAsyncFlowOption*/))
                    {
                        action(scope);
                    }
                },
                TimeSpan.MaxValue,
                ex =>
                {
                    if (ex is TransactionAbortedException &&
                        ex.InnerException is ConcurrencyException)
                    {
                        var msg = string.Format("Transaction concurrency fault occured. Attempt: {0}/{1}", attempt, maxRetries);
                        Log.Warn(msg, ex);

                        if (attempt < maxRetries)
                        {
                            return true;
                        }
                    }

                    return false;
                });
        }
    }
}