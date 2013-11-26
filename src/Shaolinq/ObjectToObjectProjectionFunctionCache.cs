// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Platform;

namespace Shaolinq
{
	internal static class ObjectToObjectProjectionFunctionCache<T, U>
	{
		private static volatile Func<ProjectionContext, T, U> CachedFunction;

		public static Func<ProjectionContext, T, U> GetCachedFunction()
		{
			Func<ProjectionContext, T, U> retval;

			if (CachedFunction == null)
			{
				retval = ObjectToObjectProjector<T, U>.Default.BuildProjectIntoNew();

				CachedFunction = retval;
			}
			else
			{
				retval = CachedFunction;
			}

			return retval;
		}
	}
}
