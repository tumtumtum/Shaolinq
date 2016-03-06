// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
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
			return this.GetExpression == null ? null : ComputedExpressionParser.Parse(this.GetExpression, propertyInfo, this.ReferencedTypes);
		}

		public LambdaExpression GetSetLambdaExpression(PropertyInfo propertyInfo)
		{
			return this.SetExpression == null ? null : ComputedExpressionParser.Parse(this.SetExpression, propertyInfo, this.ReferencedTypes);
		}
	}
}