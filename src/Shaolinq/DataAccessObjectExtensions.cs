// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static IDataAccessObjectInternal ToObjectInternal(this DataAccessObject value)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return value as IDataAccessObjectInternal;
			// ReSharper restore SuspiciousTypeConversion.Global
		}

		public static T Inflate<T>(this T dataAccessObject)
			where T : DataAccessObject
		{
			if (!((IDataAccessObjectAdvanced)dataAccessObject).IsDeflatedReference)
			{
				return dataAccessObject;
			}

			var inflated = dataAccessObject.dataAccessModel.Inflate(dataAccessObject);
			dataAccessObject.ToObjectInternal()?.SwapData(inflated, true);

			return dataAccessObject;
		}

		public static Task<T> InflateAsync<T>(this T dataAccessObject)
			where T : DataAccessObject
		{
			return dataAccessObject.InflateAsync<T>(CancellationToken.None);
		}

		public static async Task<T> InflateAsync<T>(this T dataAccessObject, CancellationToken cancellationToken)
			where T : DataAccessObject
		{
			if (!((IDataAccessObjectAdvanced)dataAccessObject).IsDeflatedReference)
			{
				return dataAccessObject;
			}

			var inflated = await dataAccessObject.dataAccessModel.InflateAsync(dataAccessObject, cancellationToken);
			dataAccessObject.ToObjectInternal().SwapData(inflated, true);

			return dataAccessObject;
		}

		internal static void SetColumnValue<T, U>(this T obj, string columnName, U value)
			where T : DataAccessObject
		{
			throw new NotImplementedException();
		}
	}
}
