using System;

namespace Shaolinq
{
	public class MissingObjectIdentifierException
		: Exception
	{
		public IDataAccessObject DataAccessObject { get; private set; }

		public MissingObjectIdentifierException(IDataAccessObject dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
