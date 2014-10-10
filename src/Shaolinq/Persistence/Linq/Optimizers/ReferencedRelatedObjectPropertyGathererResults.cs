// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class IncludedPropertyInfo
	{
		public Expression RootExpression { get; set; }
		public PropertyInfo[] PropertyPath { get; set; }
		public PropertyInfo[] SuffixPropertyPath { get; set; }
	}

	public class IncludedPropertyInfoEqualityComparer
		: IEqualityComparer<IncludedPropertyInfo>
	{
		public static readonly IncludedPropertyInfoEqualityComparer Default = new IncludedPropertyInfoEqualityComparer();

		public bool Equals(IncludedPropertyInfo x, IncludedPropertyInfo y)
		{
			if (x == y)
			{
				return true;
			}

			return x.RootExpression == y.RootExpression && ArrayEqualityComparer<PropertyInfo>.Default.Equals(x.PropertyPath, y.PropertyPath);
		}

		public int GetHashCode(IncludedPropertyInfo obj)
		{
			return obj.PropertyPath.Aggregate(obj.RootExpression.GetHashCode(), (c, d) => c ^ d.GetHashCode());
		}
	}

	public struct ReferencedRelatedObjectPropertyGathererResults
	{
		public Expression ReducedExpression { get; set; }
		public Dictionary<PropertyInfo[], Expression> RootExpressionsByPath { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfoByExpression { get; set; }
		public Dictionary<PropertyInfo[], ReferencedRelatedObject> ReferencedRelatedObjectByPath { get; set; }
	}
}