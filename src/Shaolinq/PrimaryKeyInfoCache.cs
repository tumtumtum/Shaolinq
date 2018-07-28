// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	internal static class PrimaryKeyInfoCache<U>
	{
		public static readonly MethodInfo EnumerableContainsMethod = MethodInfoFastRef.EnumerableContainsMethod.MakeGenericMethod(typeof(U));
	}
}