using System;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static T Include<T, U>(this T obj, Func<T, U> include)
			where T : IDataAccessObject
		{
			return obj;
		}
	}
}
