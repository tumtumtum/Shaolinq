// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Computed;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ComputedMemberAttribute
		: Attribute
	{
		public string GetExpression { get; set; }
		public string SetExpression { get; set; }
		public Type ReferencedType { get; set; }
		public Type[] ReferencedTypes { get; set; }
		
		public ComputedMemberAttribute(string getExpression, string setExpression = null)
		{
			this.GetExpression = getExpression;
	        this.SetExpression = setExpression;
		}

		private Type[] GetReferencedTypes(PropertyInfo propertyInfo)
		{
			var referencedTypes = new List<Type>();

			if (this.ReferencedTypes != null)
			{
				referencedTypes.AddRange(this.ReferencedTypes);
			}

			if (this.ReferencedType != null)
			{
				referencedTypes.Add(this.ReferencedType);
			}

			if (propertyInfo?.PropertyType != null)
			{
				referencedTypes.Add(propertyInfo.PropertyType);
			}

			if (propertyInfo?.DeclaringType != null)
			{
				referencedTypes.Add(propertyInfo.DeclaringType);
			}

			return referencedTypes.ToArray();
		}

		public LambdaExpression GetGetLambdaExpression(PropertyInfo propertyInfo)
		{
			return this.GetExpression == null ? null : ComputedExpressionParser.Parse(this.GetExpression, propertyInfo, GetReferencedTypes(propertyInfo));
		}

		public LambdaExpression GetSetLambdaExpression(PropertyInfo propertyInfo)
		{
			return this.SetExpression == null ? null : ComputedExpressionParser.Parse(this.SetExpression, propertyInfo, GetReferencedTypes(propertyInfo));
		}
	}
}