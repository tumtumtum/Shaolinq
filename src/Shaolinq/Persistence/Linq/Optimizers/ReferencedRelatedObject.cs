// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ReferencedRelatedObject
	{
		public PropertyPath PropertyPath { get; private set; }
		public PropertyPath SuffixPropertyPath { get; private set; }
		public HashSet<Expression> TargetExpressions { get; private set; }

		public ReferencedRelatedObject(PropertyPath propertyPath, PropertyPath suffix)
		{
			this.PropertyPath = propertyPath;
			this.SuffixPropertyPath = suffix;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}

		public ReferencedRelatedObject(PropertyPath propertyPath)
		{
			this.PropertyPath = propertyPath;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}

		public override int GetHashCode()
		{
			return this.PropertyPath.Path.Aggregate(this.PropertyPath.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}

			var value = obj as ReferencedRelatedObject;

			return value != null 
				&& PropertyPathEqualityComparer.Default.Equals(this.PropertyPath, value.PropertyPath);
		}
	}
}