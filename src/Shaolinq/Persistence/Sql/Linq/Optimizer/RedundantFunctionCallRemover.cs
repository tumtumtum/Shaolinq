using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Platform;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	public class RedundantFunctionCallRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new RedundantFunctionCallRemover().Visit(expression);		
		}

		private bool IsEmpty(IEnumerable enumerable)
		{
			var enumerator = enumerable.GetEnumerator();

			while (enumerator.MoveNext())
			{
				return false;
			}

			return true;
		}

		private static Dictionary<Type, Func<object, int>> GenericListCountGetters = new Dictionary<Type,Func<object,int>>();
        
		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.In)
			{
				var value = functionCallExpression.Arguments[1] as ConstantExpression;
				var placeholderValue = functionCallExpression.Arguments[1] as SqlConstantPlaceholderExpression;
				
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

						int count;
						Func<object, int> getter;

						if (GenericListCountGetters.TryGetValue(value.Type, out getter))
						{
							count = getter(value.Value);	
						}

						var newGenericListCountGetters = new Dictionary<Type, Func<object, int>>();
                        
						var type = value.Type.WalkHierarchy(true, false).FirstOrDefault(c => c.Name == typeof(ICollection<>).Name);

						var prop = type.GetProperty("Count");

						foreach (var kv in GenericListCountGetters)
						{
							newGenericListCountGetters[kv.Key] = kv.Value;
						}

						var param = Expression.Parameter(typeof(object), "value");
						Expression body = Expression.Convert(param, value.Type);
						body = Expression.Property(body, prop);
						getter = (Func<object, int>)Expression.Lambda(body, param).Compile();

						count = getter(value.Value);

						newGenericListCountGetters[value.Type] = getter;

						GenericListCountGetters = newGenericListCountGetters;

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
				bool ok = true;
				
				foreach (var arg in functionCallExpression.Arguments)
				{
					var constantExpression = arg as ConstantExpression;

					if (constantExpression == null)
					{
						ok = false;

						break;
					}
				}

				if (ok)
				{
					string result;
					var args = functionCallExpression.Arguments;

					switch (functionCallExpression.Arguments.Count)
					{
						case 2:
							result = String.Concat((string)((ConstantExpression)args[0]).Value, (string)((ConstantExpression)args[1]).Value);
							break;
						case 3:
							result = String.Concat((string)((ConstantExpression)args[0]).Value, (string)((ConstantExpression)args[1]).Value, (string)((ConstantExpression)args[2]).Value);
							break;
						case 4:
							result = String.Concat((string)((ConstantExpression)args[0]).Value, (string)((ConstantExpression)args[1]).Value, (string)((ConstantExpression)args[2]).Value, (string)((ConstantExpression)args[3]).Value);
							break;
						default:
							var builder = new StringBuilder();
							foreach (var arg in functionCallExpression.Arguments)
							{
								var constantExpression = (ConstantExpression)arg;
								var value = (string)constantExpression.Value;

								builder.Append(value);
							}
							result = builder.ToString();
							break;
					}

					return Expression.Constant(result);
				}
			}
			
			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
