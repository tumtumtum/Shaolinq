// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Platform;

namespace Shaolinq
{
	internal static class TranslateToHelperClass
	{
		internal static readonly MethodInfo TranslateToHelperMethod = typeof(TranslateToHelperClass).GetMethod("TranslateToHelper", BindingFlags.Static | BindingFlags.NonPublic);

		internal static DESTINATION_TYPE TranslateToHelper<SOURCE_TYPE, DESTINATION_TYPE>(ProjectionContext projectionContext, SOURCE_TYPE dataAccessObject)
		{
			var function = ObjectToObjectProjectionFunctionCache<SOURCE_TYPE, DESTINATION_TYPE>.GetCachedFunction();

			return function(projectionContext, dataAccessObject);
		}
	}

	internal static class ObjectToObjectProjectionProxyFunctionCache<U>
	{	
		private static volatile Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>> TranslateToCache = new Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>>();

		public static Func<ProjectionContext, IDataAccessObject, U> GetProxyFunction(Type sourceType)
		{
			var key = sourceType;
			Func<ProjectionContext, IDataAccessObject, U> retval;

			if (!TranslateToCache.TryGetValue(key, out retval))
			{
				var translationContextParameter = Expression.Parameter(typeof(ProjectionContext), "projectionContext");
				var parameter = Expression.Parameter(typeof(IDataAccessObject), "dataAccessObject");
				var argument = Expression.Convert(parameter, sourceType);
				var body = Expression.Call(null, TranslateToHelperClass.TranslateToHelperMethod.MakeGenericMethod(sourceType, typeof(U)), translationContextParameter, argument);
				var proxy = Expression.Lambda(body, translationContextParameter, parameter);

				retval = (Func<ProjectionContext, IDataAccessObject, U>)proxy.Compile();

				var newCache = new Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>>(TranslateToCache);

				newCache[key] = retval;

				TranslateToCache = newCache;
			}

			return retval;
		}
	}
}
