using System;

namespace Shaolinq
{
	public class InvalidPropertyAccessException
		: Exception
	{
		public InvalidPropertyAccessException(string propertyName)
			: base(String.Concat("Invalid access to property: ", propertyName))
		{
		}
	}
}
