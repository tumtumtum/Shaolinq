// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public struct ReferencedRelatedObject
	{
		public PropertyPath IncludedPropertyPath { get; }
		public PropertyPath FullAccessPropertyPath { get; }
		public Expression ObjectExpression { get; }
		public ICollection<Expression> TargetExpressions { get; }

		public ReferencedRelatedObject(PropertyPath fullAccessPropertyPath, PropertyPath includedPropertyPath, Expression objectExpression)
			: this()
		{
			this.ObjectExpression = objectExpression;
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.IncludedPropertyPath = includedPropertyPath;
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}
	}
}