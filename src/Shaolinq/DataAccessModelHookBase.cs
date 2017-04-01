using System;

namespace Shaolinq
{
	public abstract class DataAccessModelHookBase : IDataAccessModelHook
	{
		public virtual Guid? CreateGuid()
		{
			return null;
		}

		public virtual void Create(DataAccessObject dataAccessObject)
		{
		}

		public virtual void Read(DataAccessObject dataAccessObject)
		{
		}

		public virtual void BeforeSubmit(DataAccessModelHookSubmitContext context)
		{
		}

		public virtual void AfterSubmit(DataAccessModelHookSubmitContext context)
		{
		}
	}
}