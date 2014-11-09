// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public struct ReferencedRelatedObject
	{
		public PropertyPath IncludedPropertyPath { get; private set; }
		public PropertyPath FullAccessPropertyPath { get; private set; }
		public Expression ObjectExpression { get; private set; }
		public ICollection<Expression> TargetExpressions { get; private set; }

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