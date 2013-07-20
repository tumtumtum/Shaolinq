using System;

namespace Shaolinq
{
	public class WriteOnlyDataAccessObjectException
		: Exception
	{
		public IDataAccessObject DataAccessObject { get; private set; }

		public WriteOnlyDataAccessObjectException(IDataAccessObject dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
