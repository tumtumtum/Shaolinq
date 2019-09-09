// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;
using Platform;

namespace Shaolinq
{
	public partial class DataAccessModel
	{
		internal IDataAccessModelHook[] hooks = null;
		
		public void AddHook(IDataAccessModelHook value)
		{
			lock (this.hooksLock)
			{
				if (this.hooks == null)
				{
					this.hooks = new [] { value };
				}
				else
				{
					this.hooks = this.hooks.Where(c => c != value).Concat(value).ToArray();
				}
			}
		}

		public void RemoveHook(IDataAccessModelHook hook)
		{
			lock (this.hooksLock)
			{
				if (this.hooks == null || this.hooks.Length == 0)
				{
					return;
				}

				var array = this.hooks.Where(c => c != hook).ToArray();

				if (array.Length == 0)
				{
					this.hooks = null;
				}
				else
				{
					this.hooks = array;
				}
			}
		}

		public Guid CreateGuid(PropertyDescriptor propertyDescriptor)
		{
			var localHooks = this.hooks;

			if (localHooks == null)
			{
				return Guid.NewGuid();
			}

			if (localHooks.Length == 1)
			{
				return localHooks[0].CreateGuid(propertyDescriptor) ?? Guid.NewGuid();
			}

			foreach (var hook in localHooks)
			{
				var result = hook.CreateGuid(propertyDescriptor);

				if (result != null)
				{
					return result.Value;
				}
			}

			return Guid.NewGuid();
		}
		
		void IDataAccessModelInternal.OnHookCreate(DataAccessObject obj)
		{
			CallHooks(hook => hook.Create(obj));
		}

		Task IDataAccessModelInternal.OnHookCreateAsync(DataAccessObject dataAccessObject)
		{
			return ((IDataAccessModelInternal)this).OnHookCreateAsync(dataAccessObject, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookCreateAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook =>
			{
				var task = hook.CreateAsync(dataAccessObject, cancellationToken);
				task.ConfigureAwait(false);
				return task;
			});
		}
		
		void IDataAccessModelInternal.OnHookRead(DataAccessObject obj)
		{
			CallHooks(hook => hook.Read(obj));
		}

		Task IDataAccessModelInternal.OnHookReadAsync(DataAccessObject dataAccessObject)
		{
			return ((IDataAccessModelInternal)this).OnHookReadAsync(dataAccessObject, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookReadAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook => hook.ReadAsync(dataAccessObject, cancellationToken));
		}
		
		void IDataAccessModelInternal.OnHookBeforeSubmit(DataAccessModelHookSubmitContext context)
		{
			CallHooks(hook => hook.BeforeSubmit(context));
		}
		
		Task IDataAccessModelInternal.OnHookBeforeSubmitAsync(DataAccessModelHookSubmitContext context)
		{
			return ((IDataAccessModelInternal)this).OnHookBeforeSubmitAsync(context, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookBeforeSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook => hook.BeforeSubmitAsync(context, cancellationToken));
		}

		void IDataAccessModelInternal.OnHookAfterSubmit(DataAccessModelHookSubmitContext context)
		{
			CallHooks(hook => hook.AfterSubmit(context));
		}

		Task IDataAccessModelInternal.OnHookAfterSubmitAsync(DataAccessModelHookSubmitContext context)
		{
			return ((IDataAccessModelInternal)this).OnHookAfterSubmitAsync(context, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookAfterSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook => hook.AfterSubmitAsync(context, cancellationToken));
		}

		void IDataAccessModelInternal.OnHookBeforeRollback()
		{
			CallHooks(hook => hook.BeforeRollback());
		}

		Task IDataAccessModelInternal.OnHookBeforeRollbackAsync()
		{
			return ((IDataAccessModelInternal)this).OnHookBeforeRollbackAsync(CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookBeforeRollbackAsync(CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook => hook.BeforeRollbackAsync(cancellationToken));
		}

		void IDataAccessModelInternal.OnHookAfterRollback()
		{
			CallHooks(hook => hook.AfterRollback());
		}

		Task IDataAccessModelInternal.OnHookAfterRollbackAsync()
		{
			return ((IDataAccessModelInternal)this).OnHookAfterRollbackAsync(CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookAfterRollbackAsync(CancellationToken cancellationToken)
		{
			return CallHooksAsync(hook => hook.AfterRollbackAsync(cancellationToken));
		}

		private void CallHooks(Action<IDataAccessModelHook> hookAction)
		{
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					hookAction(hook);
				}
			}
		}

		private async Task CallHooksAsync(Func<IDataAccessModelHook, Task> hookFunc)
		{
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				await Task.WhenAll(localHooks.Select(hookFunc));
			}
		}
	}
}