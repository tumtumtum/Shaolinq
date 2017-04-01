using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

					Array.Copy(this.hooks, array, this.hooks.Length + 1);
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

		public Guid CreateGuid()
		{
			var localHooks = this.hooks;

			if (localHooks == null)
			{
				return Guid.NewGuid();
			}

			if (localHooks.Length == 1)
			{
				return localHooks[0].CreateGuid() ?? Guid.NewGuid();
			}

			foreach (var hook in localHooks)
			{
				var result = hook.CreateGuid();

				if (result != null)
				{
					return result.Value;
				}
			}

			return Guid.NewGuid();
		}

		internal void OnHookCreate(DataAccessObject obj)
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

		internal void OnHookRead(DataAccessObject obj)
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

		internal void OnHookBeforeSubmit(DataAccessModelHookSubmitContext context)
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

		internal void OnHookAfterSubmit(DataAccessModelHookSubmitContext context)
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
	}
}