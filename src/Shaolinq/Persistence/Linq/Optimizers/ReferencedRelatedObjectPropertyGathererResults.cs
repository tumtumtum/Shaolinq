// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
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

			return x.RootExpression == y.RootExpression
			       && PropertyPathEqualityComparer.Default.Equals(x.FullAccessPropertyPath, y.FullAccessPropertyPath);
		}

		public int GetHashCode(IncludedPropertyInfo obj)
		{
			return obj.FullAccessPropertyPath.Aggregate(obj.RootExpression.GetHashCode(), (c, d) => c ^ d.GetHashCode());
		}
	}

	public struct ReferencedRelatedObjectPropertyGathererResults
	{
		public Expression[] ReducedExpressions { get; set; }
		public Dictionary<PropertyPath, Expression> RootExpressionsByPath { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfoByExpression { get; set; }
		public Dictionary<PropertyPath, ReferencedRelatedObject> ReferencedRelatedObjectByPath { get; set; }
	}
}