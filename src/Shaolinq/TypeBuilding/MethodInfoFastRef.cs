// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.TypeBuilding
{
	public static class MethodInfoFastRef
	{
		public static readonly MethodInfo EnumerableCountMethod = TypeUtils.GetMethod(() => new List<string>().Count()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableContainsMethod = TypeUtils.GetMethod(() => Enumerable.Contains(new List<string>(), string.Empty)).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableDefaultIfEmptyMethod = TypeUtils.GetMethod(() => new List<string>().DefaultIfEmpty()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableCountMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).Count()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableWhereMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).Where(c => c.Length == 0)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableSelectMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).Select(c => c.ToUpper())).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableSelectManyMethod = TypeUtils.GetMethod(() => ((IQueryable<Tuple<IEnumerable<int>, string>>)null).SelectMany(c => c.Item1, (x, y) => new { x, y })).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableJoinMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).Join(((IQueryable<int>)null), c => c.Length, c => c, (x, y) => x)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupJoinMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).GroupJoin(((IQueryable<int>)null), c => c.Length, c => c, (x, y) => x)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableDefaultIfEmptyMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).DefaultIfEmpty()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableExtensionsIncludeMethod = TypeUtils.GetMethod(() => QueryableExtensions.Include(((IQueryable<DataAccessObject>)null), c => c)).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessObjectExtensionsIncludeMethod = TypeUtils.GetMethod(() => ((DataAccessObject)null).Include(c => c)).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumParseMethod = TypeUtils.GetMethod(() => Enum.Parse(typeof(Enum), ""));
		public static readonly MethodInfo DataAccessModelGetReferenceByPrimaryKeyWithPrimaryKeyValuesMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.GetReference<DataAccessObject>(new object[0])).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessModelGenericInflateMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.Inflate<DataAccessObject>((DataAccessObject)null)).GetGenericMethodDefinition();
		public static readonly MethodInfo GuidEqualsMethod = TypeUtils.GetMethod<Guid>(c => c.Equals(Guid.Empty));
		public static readonly MethodInfo GuidNewGuid = TypeUtils.GetMethod(() => Guid.NewGuid());
		public static readonly MethodInfo StringExtensionsIsLikeMethodInfo = TypeUtils.GetMethod(() => string.Empty.IsLike(string.Empty));
		public static readonly MethodInfo ObjectToStringMethod = TypeUtils.GetMethod<object>(c => c.ToString());
		public static readonly MethodInfo EnumToObjectMethod = TypeUtils.GetMethod(() => Enum.ToObject(typeof(Enum), 0));
		public static readonly MethodInfo EnumerableFirstMethod = TypeUtils.GetMethod(() => ((IEnumerable<string>)null).First());
		public static readonly MethodInfo StringStaticEqualsMethod = TypeUtils.GetMethod(() => string.Equals(string.Empty, string.Empty));
		public static readonly MethodInfo ObjectEqualsMethod = TypeUtils.GetMethod<object>(c => c.Equals((object)null));
		public static readonly MethodInfo ObjectStaticEqualsMethod = TypeUtils.GetMethod(() => object.Equals((object)1, (object)2));
		public static readonly MethodInfo ObjectStaticReferenceEqualsMethod = TypeUtils.GetMethod(() => object.ReferenceEquals((object)1, (object)2));
		public static readonly MethodInfo StringConcatMethod2 = TypeUtils.GetMethod(() => string.Concat("", ""));
		public static readonly MethodInfo StringConcatMethod3 = TypeUtils.GetMethod(() => string.Concat("", "", ""));
		public static readonly MethodInfo StringConcatMethod4 = TypeUtils.GetMethod(() => string.Concat("", "", "", ""));
		public static readonly MethodInfo TypeGetTypeFromHandle = TypeUtils.GetMethod(() => Type.GetTypeFromHandle(Type.GetTypeHandle(new object())));
		public static readonly MethodInfo ConvertChangeTypeMethod = TypeUtils.GetMethod(() => Convert.ChangeType(null, typeof(string)));
		public static readonly MethodInfo ObjectPropertyValueListAddMethod = TypeUtils.GetMethod<List<ObjectPropertyValue>>(c => c.Add(default(ObjectPropertyValue)));
		public static readonly MethodInfo DataAccessModelGetReference = TypeUtils.GetMethod<DataAccessModel>(c => c.GetReference<DataAccessObject, int>(0, PrimaryKeyType.Composite)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupByMethod = TypeUtils.GetMethod(() => ((IQueryable<object>)null).GroupBy<object, string>((Expression<Func<object, string>>)null)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupByWithElementSelectorMethod = TypeUtils.GetMethod(() => ((IQueryable<object>)null).GroupBy<object, string, int>((Expression<Func<object, string>>)null, (Expression<Func<object, int>>)null)).GetGenericMethodDefinition();
		public static readonly MethodInfo SqlQueryProviderProcessResultMethod = TypeUtils.GetMethod(() => SqlQueryProvider.ProcessResult(default(SqlQueryProvider.PrivateExecuteResult<int>))).GetGenericMethodDefinition();
	}
}
