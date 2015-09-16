using System;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Parser;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ComputedMemberAttribute
		: Attribute
	{
		public string Expression { get; set; }
		
		public ComputedMemberAttribute(string expression)
		{
			this.Expression = expression;
		}

		public LambdaExpression GetLambdaExpression(PropertyInfo propertyInfo)
		{
			return ComputedExpressionParser.Parse(Expression, propertyInfo);
		}
	}
}