// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Reflection;

namespace Shaolinq
{
	internal static class PrimaryKeyInfoCache<U>
	{
		public static readonly PropertyInfo IdPropertyInfo = typeof(U).GetProperties().FirstOrDefault(c => c.Name == "Id");
		public static readonly MethodInfo EnumerableContainsMethod = MethodInfoFastRef.EnumerableContainsMethod.MakeGenericMethod(typeof(U));
	}
}