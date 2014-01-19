// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq
{
	internal class TypeHelper
	{
		public static readonly Type ListType = typeof(List<>);
		public static readonly Type IListType = typeof(IList<>);
		public static readonly Type IEnumerableType = typeof(IEnumerable<>);
		public static readonly Type DataAccessObjectType = typeof(DataAccessObject<>); 
		public static readonly Type DataAccessObjectsType = typeof(DataAccessObjects<>);
		public static readonly Type RelatedDataAccessObjectsType = typeof(RelatedDataAccessObjects<>);
		public static readonly Type IQueryableType = typeof(IQueryable<>);

		public static T ConvertValue<T>(string value)
		{
			if (typeof(T).IsEnum)
			{
				return (T)Enum.Parse(typeof(T), value);
			}
			else if (typeof(T) == typeof(Guid))
			{
				return (T)((object)new Guid(value));
			}
			else
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}
		}

		public static Type GetSequenceType(Type elementType)
		{
			return typeof(IEnumerable<>).MakeGenericType(elementType);
		}

		public static Type GetElementType(Type sequenceType)
		{
			var retval = FindElementType(sequenceType);

			return retval ?? sequenceType;
		}

		private static Type FindElementType(Type sequenceType)
		{
			if (sequenceType == null || sequenceType == typeof(string))
			{
				return null;
			}

			if (sequenceType.IsArray)
			{
				return sequenceType.GetElementType();
			}

			if (sequenceType.IsGenericType)
			{
				var genericType = sequenceType.GetGenericTypeDefinition();

				if (genericType == DataAccessObjectType)
				{
					return null;
				}
			
				if (genericType == ListType || genericType == IListType || genericType == DataAccessObjectsType || genericType == RelatedDataAccessObjectsType)
				{
					return sequenceType.GetGenericArguments()[0];
				}

				foreach (var genericArgument in sequenceType.GetGenericArguments())
				{
					var iEnumerable = typeof(IEnumerable<>).MakeGenericType(genericArgument);

					if (iEnumerable.IsAssignableFrom(sequenceType))
					{
						return genericArgument;
					}
				}
			}

			var interfaces = sequenceType.GetInterfaces();

			if (interfaces.Length > 0)
			{
				foreach (var interfaceType in interfaces)
				{
					var element = FindElementType(interfaceType);

					if (element != null)
					{
						return element;
					}
				}
			}

			if (sequenceType.BaseType != null && sequenceType.BaseType != typeof(object))
			{
				return FindElementType(sequenceType.BaseType);
			}

			return null;
		}
	}
}
