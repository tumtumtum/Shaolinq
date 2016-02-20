// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq
{
	public class InvalidPrimaryKeyPropertyAccessException
		: InvalidPropertyAccessException
	{
		public InvalidPrimaryKeyPropertyAccessException(string propertyName)
			: base(propertyName)
		{
		}
	}
}
