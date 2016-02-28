// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		internal static P AddToCollection<P, C>(P parent, Func<P, RelatedDataAccessObjects<C>> getChildren, C child, int version)
			where P : DataAccessObject
			where C : DataAccessObject
		{
			if (parent == null)
			{
				return null;
			}

			getChildren(parent).Add(child, version);

			return parent;
		}
		
		internal static T Include<T, U>(this T obj, Expression<Func<T, U>> include)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return obj;
			// ReSharper restore SuspiciousTypeConversion.Global
		}

		internal static IDataAccessObjectInternal ToObjectInternal(this DataAccessObject value)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return (IDataAccessObjectInternal)value;
			// ReSharper restore SuspiciousTypeConversion.Global
		}

		public static T Inflate<T>(this T dataAccessObject)
			where T : DataAccessObject
		{
			if (!((IDataAccessObjectAdvanced)dataAccessObject).IsDeflatedReference)
			{
				return dataAccessObject;
			}

			var inflated = dataAccessObject.dataAccessModel.Inflate((DataAccessObject)dataAccessObject);
			dataAccessObject.ToObjectInternal().SwapData(inflated, true);

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

			var inflated = await dataAccessObject.dataAccessModel.InflateAsync((DataAccessObject)dataAccessObject, cancellationToken);
			dataAccessObject.ToObjectInternal().SwapData(inflated, true);

			return dataAccessObject;
		}
	}
}
