using System;

namespace Shaolinq
{
	public class MissingPropertyValueException
		: DataAccessException
	{
		public IDataAccessObject RelatedObject { get; private set; }

		public MissingPropertyValueException(IDataAccessObject relatedObject, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.RelatedObject = relatedObject;
		}
	}
}
