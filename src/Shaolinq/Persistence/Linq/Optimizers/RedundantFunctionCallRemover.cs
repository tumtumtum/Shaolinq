// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Platform;
using Platform.Collections;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class RedundantFunctionCallRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new RedundantFunctionCallRemover().Visit(expression);		
		}

		private static bool IsEmpty(IEnumerable enumerable)
		{
			var enumerator = enumerable.GetEnumerator();

			while (enumerator.MoveNext())
			{
				return false;
			}

			return true;
		}

		private static Dictionary<Type, Func<object, int>> genericListCounters = new Dictionary<Type,Func<object,int>>();
        
		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.IsNull)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant)
				{
					return Expression.Constant(((ConstantExpression) functionCallExpression.Arguments[0]).Value == null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.IsNotNull)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant)
				{
					return Expression.Constant(((ConstantExpression)functionCallExpression.Arguments[0]).Value != null);
				}

				return functionCallExpression;
			}
			else if (functionCallExpression.Function == SqlFunction.In)
			{
				var value = this.Visit(functionCallExpression.Arguments[1]) as ConstantExpression;
				var placeholderValue = this.Visit(functionCallExpression.Arguments[1]) as SqlConstantPlaceholderExpression;
				
				if (placeholderValue != null)
				{
					value = placeholderValue.ConstantExpression;
				}

				if (value != null)
				{
					if (value.Type.IsArray)
					{
						if (((Array)value.Value).Length == 0)
						{
							return Expression.Constant(false, functionCallExpression.Type);
						}
					}
					else if (typeof(ICollection<>).IsAssignableFromIgnoreGenericParameters(value.Type))
					{
						// TODO: Use cached dynamic method instead of reflection call

						Func<object, int> counter = null;

						if (!genericListCounters.TryGetValue(value.Type, out counter))
						{

							var type = value.Type.WalkHierarchy(true, false).FirstOrDefault(c => c.Name == typeof(ICollection<>).Name);

							if (type != null)
							{
								var prop = type.GetProperty("Count");

								var param = Expression.Parameter(typeof(object), "value");
								Expression body = Expression.Convert(param, value.Type);
								body = Expression.Property(body, prop);
								counter = (Func<object, int>) Expression.Lambda(body, param).Compile();

								genericListCounters = new Dictionary<Type, Func<object, int>>(genericListCounters) {[value.Type] = counter};
							}
						}

						var count = counter?.Invoke(value.Value);

						if (count == 0)
						{
							return Expression.Constant(false, functionCallExpression.Type);
						}
					}
					else if (typeof(IEnumerable).IsAssignableFrom(value.Type))
					{
						if (IsEmpty((IEnumerable)value.Value))
						{
							return Expression.Constant(false, functionCallExpression.Type);
						}
					}
				}

				return functionCallExpression;	
			}
			else if (functionCallExpression.Function == SqlFunction.Concat)
			{
				var visitedArguments = this.VisitExpressionList(functionCallExpression.Arguments);
				
				if (visitedArguments.All(c => c is ConstantExpression))
				{
					string result;
					
					switch (functionCallExpression.Arguments.Count)
					{
					case 2:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value);
						break;
					case 3:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value, (string)((ConstantExpression)visitedArguments[2]).Value);
						break;
					case 4:
						result = string.Concat((string)((ConstantExpression)visitedArguments[0]).Value, (string)((ConstantExpression)visitedArguments[1]).Value, (string)((ConstantExpression)visitedArguments[2]).Value, (string)((ConstantExpression)visitedArguments[3]).Value);
						break;
					default:
						result = visitedArguments
								.Cast<ConstantExpression>()
								.Select(c => c.Value)
								.Aggregate(new StringBuilder(), (s, c) => s.Append(c))
								.ToString();
						break;
					}

					return Expression.Constant(result);
				}
			}
			
			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
