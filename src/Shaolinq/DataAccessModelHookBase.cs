using System;

namespace Shaolinq
{
	public class DataAccessModelHookBase : IDataAccessModelHook
	{
		public Guid? CreateGuid()
		{
			return null;
		}

		public void Create(DataAccessObject dataAccessObject)
		{
		}

		public void Read(DataAccessObject dataAccessObject)
		{
		}

		public void BeforeSubmit(DataAccessModelHookSubmitContext context)
		{
		}

		public void AfterSubmit(DataAccessModelHookSubmitContext context)
		{
		}
	}
}