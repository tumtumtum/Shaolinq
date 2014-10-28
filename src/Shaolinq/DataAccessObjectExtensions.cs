using System;
using log4net;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DataAccessObjectExtensions));

		public static T Include<T, U>(this T obj, Func<T, U> include)
			where T : DataAccessObject
			where U : DataAccessObject
		{
			Log.ErrorFormat("Include called on object ({0}) rather than within a Select or LINQ query", obj);
			
			return (T)include(obj).Inflate();
		}

		internal static IDataAccessObjectInternal ToObjectInternal(this DataAccessObject value)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return (IDataAccessObjectInternal)value;
			// ReSharper restore SuspiciousTypeConversion.Global
		}
	}
}
