// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq
{
	public static class MethodInfoFastRef
	{
		public static readonly MethodInfo EnumerableCountMethod = typeof(Enumerable).GetMethods().Single(c => c.Name == "Count" && c.GetParameters().Length == 1);
		public static readonly MethodInfo EnumerableContainsMethod = typeof(Enumerable).GetMethods().Single(c => c.Name == "Contains" && c.GetParameters().Length == 2);
		public static readonly MethodInfo QueryableCountMethod = typeof(Queryable).GetMethods().Single(c => c.Name == "Count" && c.GetParameters().Length == 1);
		public static readonly MethodInfo QueryableWhereMethod = (from method in typeof(Queryable).GetMethods().Where(c => c.Name == "Where") let parameters = method.GetParameters() where parameters.Length == 2 let genericargs = parameters[1].ParameterType.GetGenericArguments() where genericargs.Length == 1 where genericargs[0].GetGenericArguments().Length == 2 select method).Single();
		public static readonly MethodInfo QueryableSelectMethod = typeof(Queryable).GetMethods().Where(c => c.Name == "Select").Single(c => c.GetParameters().Length == 2 && c.GetParameters()[1].ParameterType.IsGenericType && c.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);
		public static readonly MethodInfo QueryableJoinMethod = (typeof(Queryable).GetMethods().Where(c => c.Name == "Join")).Single(c => c.GetParameters().Length == 5);
		public static readonly MethodInfo QueryableDefaultIfEmptyMethod = (typeof(Queryable).GetMethods().Where(c => c.Name == "DefaultIfEmpty")).Single(c => c.GetParameters().Length == 1);
		public static readonly MethodInfo QueryableExtensionsIncludeMethod = typeof(QueryableExtensions).GetMethods().Single(c => c.Name == "Include" && c.IsGenericMethod && c.GetParameters().Length == 2 && c.ReturnType == typeof(IQueryable<>).MakeGenericType(c.GetGenericArguments()[0]));
		public static readonly MethodInfo DataAccessObjectExtensionsIncludeMethod = typeof(DataAccessObjectExtensions).GetMethods().Single(c => c.Name == "Include" && c.IsGenericMethod && c.GetParameters().Length == 2 && c.ReturnType == c.GetGenericArguments()[0]);
		public static readonly MethodInfo EnumParseMethod = typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null);
		public static readonly MethodInfo BaseDataAccessModelGetReferenceByPrimaryKeyWithPrimaryKeyValuesMethod = typeof(DataAccessModel).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(c => c.Name == "GetReference" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(object[]));
		public static readonly MethodInfo BaseDataAccessModelGenericInflateMethod = typeof(DataAccessModel).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(c => c.Name == "Inflate" && c.IsGenericMethod);
		public static readonly MethodInfo GuidEqualsMethod = typeof(Guid).GetMethod("Equals", new Type[] { typeof(Guid) });
		public static readonly MethodInfo GuidNewGuid = typeof(Guid).GetMethod("NewGuid", BindingFlags.Public | BindingFlags.Static);
		public static readonly MethodInfo StringExtensionsIsLikeMethodInfo = typeof(ShaolinqStringExtensions).GetMethod("IsLike", BindingFlags.Static | BindingFlags.Public);
		public static readonly MethodInfo StringSubstring= typeof(String).GetMethod("Substring", BindingFlags.Static | BindingFlags.Public);
		public static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
		public static readonly MethodInfo EnumToObjectMethod = typeof(Enum).GetMethod("ToObject", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Type), typeof(int) }, null);
		public static readonly MethodInfo EnumerableFirstMethod = typeof(Enumerable).GetMethods().First(c => c.Name == "First" && c.GetParameters().Length == 1);
		public static readonly MethodInfo StringStaticEqualsMethod = typeof(string).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null);
		public static readonly MethodInfo ObjectEqualsMethod = typeof(object).GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(object) }, null);
		public static readonly MethodInfo ObjectStaticEqualsMethod = typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(object), typeof(object) }, null);
		public static readonly MethodInfo StringConcatMethod2 = typeof(string).GetMethod("Concat", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null);
		public static readonly MethodInfo StringConcatMethod3 = typeof(string).GetMethod("Concat", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string), typeof(string) }, null);
		public static readonly MethodInfo StringConcatMethod4 = typeof(string).GetMethod("Concat", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
		public static readonly MethodInfo TypeGetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(RuntimeTypeHandle) }, null);
		public static readonly MethodInfo ConvertChangeTypeMethod = typeof(Convert).GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(object), typeof(Type) }, null);
		public static readonly MethodInfo DictionaryTryGetValueMethod = typeof(Dictionary<,>).GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public);
		public static readonly MethodInfo ObjectPropertyValueListAddMethod = typeof(List<ObjectPropertyValue>).GetMethod("Add");
	}
}
