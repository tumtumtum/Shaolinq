// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.TypeBuilding
{
	public static class MethodInfoFastRef
	{
		public static readonly MethodInfo TaskExtensionsUnwrapMethod = TypeUtils.GetMethod(() => System.Threading.Tasks.TaskExtensions.Unwrap<int>(null)).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableCountMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).Count()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableWhereMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).Where(c => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableContainsMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).Contains(default(string))).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableDefaultIfEmptyMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).DefaultIfEmpty()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableDefaultIfEmptyWithValueMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).DefaultIfEmpty("")).GetGenericMethodDefinition();
        public static readonly MethodInfo QueryableCountMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).Count()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableWhereMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).Where(c => c.Length == 0)).GetGenericMethodDefinition();
        public static readonly MethodInfo QueryableExtensionsDeleteMethod = TypeUtils.GetMethod(() => QueryableExtensions.Delete<DataAccessObject>(null)).GetGenericMethodDefinition();
        public static readonly MethodInfo QueryableSelectMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).Select(c => c.ToUpper())).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableSelectManyMethod = TypeUtils.GetMethod(() => default(IQueryable<Tuple<IEnumerable<int>, string>>).SelectMany(c => c.Item1, (x, y) => new { x, y })).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableJoinMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).Join(((IQueryable<int>)null), c => c.Length, c => c, (x, y) => x)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupJoinMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).GroupJoin(default(IQueryable<int>), c => c.Length, c => c, (x, y) => x)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableDefaultIfEmptyMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).DefaultIfEmpty()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableDefaultIfEmptyWithValueMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).DefaultIfEmpty(string.Empty)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableExtensionsIncludeMethod = TypeUtils.GetMethod(() => QueryableExtensions.Include(default(IQueryable<DataAccessObject>), c => c)).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessObjectExtensionsIncludeMethod = TypeUtils.GetMethod(() => ((DataAccessObject)null).Include(c => c)).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumParseMethod = TypeUtils.GetMethod(() => Enum.Parse(typeof(Enum), default(string)));
		public static readonly MethodInfo DataAccessModelGetReferenceByPrimaryKeyWithPrimaryKeyValuesMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.GetReference<DataAccessObject>(new object[0])).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessModelGenericInflateHelperMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.InflateHelper<DataAccessObject>(default(DataAccessObject))).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessModelGenericInflateAsyncHelperMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.InflateAsyncHelper<DataAccessObject>(default(DataAccessObject), CancellationToken.None)).GetGenericMethodDefinition();
		public static readonly MethodInfo GuidEqualsMethod = TypeUtils.GetMethod<Guid>(c => c.Equals(Guid.Empty));
		public static readonly MethodInfo GuidNewGuidMethod = TypeUtils.GetMethod(() => Guid.NewGuid());
		public static readonly MethodInfo StringExtensionsIsLikeMethod = TypeUtils.GetMethod(() => string.Empty.IsLike(default(string)));
		public static readonly MethodInfo ObjectToStringMethod = TypeUtils.GetMethod<object>(c => c.ToString());
		public static readonly MethodInfo EnumToObjectMethod = TypeUtils.GetMethod(() => Enum.ToObject(typeof(Enum), 0));
		public static readonly MethodInfo EnumerableExtensionsAlwaysReadFirstMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).AlwaysReadFirst()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableFirstMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).First()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableFirstOrDefaultMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).FirstOrDefault()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableSingleMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).Single()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableEmptyIfFirstIsNullMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).EmptyIfFirstIsNull()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableSingleOrSpecifiedValueIfFirstIsDefaultValueMethod = TypeUtils.GetMethod(() => default(IEnumerable<string>).SingleOrSpecifiedValueIfFirstIsDefaultValue(default(string))).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableUtilsSingleOrExceptionIfFirstIsNullMethod = TypeUtils.GetMethod(() => default(IEnumerable<int?>).SingleOrExceptionIfFirstIsNull()).GetGenericMethodDefinition();
		public static readonly MethodInfo EnumerableDefaultIfEmptyCoalesceSpecifiedValueMethod = TypeUtils.GetMethod(() => default(IEnumerable<int?>).DefaultIfEmptyCoalesceSpecifiedValue(0)).GetGenericMethodDefinition();
		public static readonly MethodInfo StringStaticEqualsMethod = TypeUtils.GetMethod(() => string.Equals(default(string), default(string)));
		public static readonly MethodInfo ObjectEqualsMethod = TypeUtils.GetMethod<object>(c => c.Equals((object)null));
		public static readonly MethodInfo ObjectStaticEqualsMethod = TypeUtils.GetMethod(() => object.Equals((object)1, (object)2));
		public static readonly MethodInfo ObjectStaticReferenceEqualsMethod = TypeUtils.GetMethod(() => object.ReferenceEquals((object)1, (object)2));
		public static readonly MethodInfo StringConcatMethod2 = TypeUtils.GetMethod(() => string.Concat(default(string), default(string)));
		public static readonly MethodInfo StringConcatMethod3 = TypeUtils.GetMethod(() => string.Concat(default(string), default(string), default(string)));
		public static readonly MethodInfo StringConcatMethod4 = TypeUtils.GetMethod(() => string.Concat(default(string), default(string), default(string)));
		public static readonly MethodInfo TypeGetTypeFromHandleMethod = TypeUtils.GetMethod(() => Type.GetTypeFromHandle(Type.GetTypeHandle(new object())));
		public static readonly MethodInfo ConvertChangeTypeMethod = TypeUtils.GetMethod(() => Convert.ChangeType(null, typeof(string)));
		public static readonly MethodInfo ObjectPropertyValueListAddMethod = TypeUtils.GetMethod<List<ObjectPropertyValue>>(c => c.Add(default(ObjectPropertyValue)));
		public static readonly MethodInfo DataAccessModelGetReferenceMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.GetReference<DataAccessObject, int>(0, PrimaryKeyType.Composite)).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessModelGetDataAccessObjectsMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.GetDataAccessObjects<DataAccessObject>()).GetGenericMethodDefinition();
		public static readonly MethodInfo DataAccessModelGetReferenceByValuesMethod = TypeUtils.GetMethod<DataAccessModel>(c => c.GetReference<DataAccessObject>(default(object[]))).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupByMethod = TypeUtils.GetMethod(() => default(IQueryable<object>).GroupBy<object, string>((Expression<Func<object, string>>)null)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableGroupByWithElementSelectorMethod = TypeUtils.GetMethod(() => default(IQueryable<object>).GroupBy<object, string, int>(default(Expression<Func<object, string>>), (Expression<Func<object, int>>)null)).GetGenericMethodDefinition();
		public static readonly MethodInfo TransactionContextGetCurrentContextVersion = TypeUtils.GetMethod(() => TransactionContext.GetCurrentTransactionContextVersion(default(DataAccessModel)));
		public static readonly MethodInfo DataAccessObjectExtensionsAddToCollectionMethod = TypeUtils.GetMethod(() => DataAccessObjectExtensions.AddToCollection<DataAccessObject, DataAccessObject>(null, null, null, 0)).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableExtensionsItemsMethod = TypeUtils.GetMethod(() => default(IQueryable<DataAccessObject>).IncludedItems()).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableExtensionsLeftJoinMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).LeftJoin(default(IEnumerable<string>), x => "", y => "", (x, y) => "")).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableOrderByMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).OrderBy(c => c.ToString())).GetGenericMethodDefinition();
		public static readonly MethodInfo QueryableThenByMethod = TypeUtils.GetMethod(() => default(IQueryable<string>).OrderBy(c => c.ToString()).ThenBy(c => c.ToString())).GetGenericMethodDefinition();
        public static readonly MethodInfo ExecutionBuildResultEvaluateMethod = TypeUtils.GetMethod<ExecutionBuildResult>(c => c.Evaluate<int>()).GetGenericMethodDefinition();
    }
}
