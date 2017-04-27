using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public abstract class DataAccessModelHookBase : IDataAccessModelHook
	{
		public Guid? CreateGuid()
		{
			return CreateGuid(null);
		}

		public virtual Guid? CreateGuid(PropertyDescriptor propertyDescriptor)
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