using System;

namespace Shaolinq
{
	public class MissingPropertyValueException
		: DataAccessException
	{
		public IDataAccessObjectAdvanced RelatedObject { get; }

		public MissingPropertyValueException(IDataAccessObjectAdvanced relatedObject, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.RelatedObject = relatedObject;
		}
	}
}
