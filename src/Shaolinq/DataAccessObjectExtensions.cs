// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		internal static Tuple<P, C> AddToCollection<P, C>(P parent, RelatedDataAccessObjects<C> children, C child, int version)
			where P : DataAccessObject
			where C : DataAccessObject
		{
			children.AddValue(child, version);

			return new Tuple<P, C>(parent, child);
		}
		
		internal static T Include<T, U>(this T obj, Func<T, U> include)
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
	}
}
