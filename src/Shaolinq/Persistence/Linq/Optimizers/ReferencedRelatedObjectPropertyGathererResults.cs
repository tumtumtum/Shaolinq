// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class IncludedPropertyInfo
	{
		public Expression RootExpression { get; set; }
		public PropertyInfo[] PropertyPath { get; set; }
	}

	public struct ReferencedRelatedObjectPropertyGathererResults
	{
		public Expression ReducedExpression { get; set; }
		public Dictionary<PropertyInfo[], Expression> RootExpressionsByPath { get; set; }
		public Dictionary<Expression, IncludedPropertyInfo> IncludedPropertyInfoByExpression { get; set; }
		public Dictionary<PropertyInfo[], ReferencedRelatedObject> ReferencedRelatedObjectByPath { get; set; }
	}
}