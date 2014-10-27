using System;

namespace Shaolinq
{
	public class ObjectAlreadyExistsException
		: UniqueConstraintException
	{
		public IDataAccessObject Object { get; private set; }

		public ObjectAlreadyExistsException(IDataAccessObject obj, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.Object = obj;
		}
	}
}
