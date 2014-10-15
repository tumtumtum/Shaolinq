using System;
using log4net;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(DataAccessObjectExtensions));

		public static T Include<T, U>(this T obj, Func<T, U> include)
			where T : IDataAccessObject
			where U : IDataAccessObject
		{
			Log.ErrorFormat("Include called on object ({0}) rather than within a Select or LINQ query", obj);
			
			return (T)include(obj).Inflate();
		}
	}
}
