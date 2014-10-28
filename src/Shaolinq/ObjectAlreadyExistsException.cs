using System;

namespace Shaolinq
{
	public class ObjectAlreadyExistsException
		: UniqueConstraintException
	{
		public IDataAccessObjectAdvanced Object { get; private set; }

		public ObjectAlreadyExistsException(IDataAccessObjectAdvanced obj, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.Object = obj;
		}
	}
}
