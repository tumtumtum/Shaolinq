// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
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
		public Type[] ReferencedTypes { get; set; }
		
        public ComputedMemberAttribute(string getExpression, string setExpression = null)
		{
			this.GetExpression = getExpression;
	        this.SetExpression = setExpression;
		}

		public LambdaExpression GetGetLambdaExpression(PropertyInfo propertyInfo)
		{
			var referencedTypes = this.ReferencedTypes.Concat(propertyInfo.PropertyType).Concat(propertyInfo.DeclaringType).ToArray();

			return this.GetExpression == null ? null : ComputedExpressionParser.Parse(this.GetExpression, propertyInfo, referencedTypes);
		}

		public LambdaExpression GetSetLambdaExpression(PropertyInfo propertyInfo)
		{
			var referencedTypes = this.ReferencedTypes.Concat(propertyInfo.PropertyType).Concat(propertyInfo.DeclaringType).ToArray();

			return this.SetExpression == null ? null : ComputedExpressionParser.Parse(this.SetExpression, propertyInfo, referencedTypes);
		}
	}
}