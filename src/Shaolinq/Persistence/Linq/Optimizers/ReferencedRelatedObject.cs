// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public struct ReferencedRelatedObject
	{
		public Expression SourceParameterExpression { get; }
		public PropertyPath IncludedPropertyPath { get; }
		public PropertyPath FullAccessPropertyPath { get; }
		public ICollection<Expression> TargetExpressions { get; }

		public ReferencedRelatedObject(PropertyPath fullAccessPropertyPath, PropertyPath includedPropertyPath, Expression sourceParameterExpression)
			: this()
		{
			this.SourceParameterExpression = sourceParameterExpression;
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.IncludedPropertyPath = includedPropertyPath;
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}
	}
}