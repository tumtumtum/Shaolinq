// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.TypeBuilding
{
	public static class ComparisonHelper
	{
		public static bool AreEqual<T>(T left, T right)
			where T : class
		{
			return Equals(left, right);
		}
	
		public static bool NullableAreEqual<T>(T? left, T? right)
			where T : struct
		{
			return left.Equals(right);
		}
	}
}