// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq
{
	public static class ShaolinqStringExtensions
	{
		public static bool IsLike(this string stringValue, string value)
		{
			return stringValue.Contains(value);
		}
	}
}
