using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial class DataAccessModel
	{
		internal IDataAccessModelHook[] hooks = null;
		
		public void AddHook(IDataAccessModelHook value)
		{
			lock (this.hooksLock)
			{
				if (this.hooks?.Contains(value) == true)
				{
					return;
				}

				if (this.hooks == null)
				{
					this.hooks = new [] { value };
				}
				else
				{
					var array = new IDataAccessModelHook[this.hooks.Length + 1];

					Array.Copy(this.hooks, array, this.hooks.Length);
					this.hooks[this.hooks.Length] = value;

					this.hooks = array;
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
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					hook.Create(obj);
				}
			}
		}

		Task IDataAccessModelInternal.OnHookCreateAsync(DataAccessObject dataAccessObject)
		{
			return ((IDataAccessModelInternal)this).OnHookCreateAsync(dataAccessObject, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookCreateAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			var localHooks = this.hooks;
			var tasks = new List<Task>();
	
			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					var task = hook.CreateAsync(dataAccessObject, cancellationToken);
					
					task.ConfigureAwait(false);
					tasks.Add(task);
				}
			}

			return Task.WhenAll(tasks);
		}
		
		void IDataAccessModelInternal.OnHookRead(DataAccessObject obj)
		{
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					hook.Read(obj);
				}
			}
		}

		Task IDataAccessModelInternal.OnHookReadAsync(DataAccessObject dataAccessObject)
		{
			return ((IDataAccessModelInternal)this).OnHookCreateAsync(dataAccessObject, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookReadAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			var localHooks = this.hooks;
			var tasks = new List<Task>();
	
			if (localHooks != null)
			{
				tasks.AddRange(localHooks.Select(hook => hook.ReadAsync(dataAccessObject, cancellationToken)));
			}

			return Task.WhenAll(tasks);
		}
		
		void IDataAccessModelInternal.OnHookBeforeSubmit(DataAccessModelHookSubmitContext context)
		{
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					hook.BeforeSubmit(context);
				}
			}
		}
		
		Task IDataAccessModelInternal.OnHookBeforeSubmitAsync(DataAccessModelHookSubmitContext context)
		{
			return ((IDataAccessModelInternal)this).OnHookBeforeSubmitAsync(context, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookBeforeSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
		{
			var localHooks = this.hooks;
			var tasks = new List<Task>();
	
			if (localHooks != null)
			{
				tasks.AddRange(localHooks.Select(hook => hook.BeforeSubmitAsync(context, cancellationToken)));
			}

			return Task.WhenAll(tasks);
		}

		void IDataAccessModelInternal.OnHookAfterSubmit(DataAccessModelHookSubmitContext context)
		{
			var localHooks = this.hooks;

			if (localHooks != null)
			{
				foreach (var hook in localHooks)
				{
					hook.AfterSubmit(context);
				}
			}
		}

		Task IDataAccessModelInternal.OnHookAfterSubmitAsync(DataAccessModelHookSubmitContext context)
		{
			return ((IDataAccessModelInternal)this).OnHookBeforeSubmitAsync(context, CancellationToken.None);
		}

		Task IDataAccessModelInternal.OnHookAfterSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
		{
			var localHooks = this.hooks;
			var tasks = new List<Task>();
	
			if (localHooks != null)
			{
				tasks.AddRange(localHooks.Select(hook => hook.AfterSubmitAsync(context, cancellationToken)));
			}

			return Task.WhenAll(tasks);
		}
	}
}