// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ReferencedRelatedObject
	{
		public PropertyPath IncludedPropertyPath { get; set; }
		public PropertyPath FullAccessPropertyPath { get; private set; }
		public HashSet<Expression> TargetExpressions { get; private set; }

		public ReferencedRelatedObject(PropertyPath fullAccessPropertyPath, PropertyPath includedPropertyPath)
		{
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.IncludedPropertyPath = includedPropertyPath;
			this.FullAccessPropertyPath = fullAccessPropertyPath;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}

		public override int GetHashCode()
		{
			return this.FullAccessPropertyPath.Path.Aggregate(this.FullAccessPropertyPath.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}

			var value = obj as ReferencedRelatedObject;

			return value != null 
				&& PropertyPathEqualityComparer.Default.Equals(this.FullAccessPropertyPath, value.FullAccessPropertyPath);
		}
	}
}