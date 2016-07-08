// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public static class ExpressionFastCompiler
	{
		private static Dictionary<Expression, SubstituteConstantsResult> cachedSubstitutedExpressions = new Dictionary<Expression, SubstituteConstantsResult>(SqlExpressionEqualityComparer.IgnoreConstants);
		private static Dictionary<SubstituteConstantsResult, Delegate> delegatesByCachedCompileResult = new Dictionary<SubstituteConstantsResult, Delegate>(ObjectReferenceIdentityEqualityComparer<SubstituteConstantsResult>.Default);

		public static object CompileAndRun(LambdaExpression expression, params object[] args)
		{
			Delegate del;

			if (args.Length != expression.Parameters.Count)
			{
				throw new ArgumentException(nameof(args));
			}

			var resultWithValues = SubstituteConstants(expression);

			if (!delegatesByCachedCompileResult.TryGetValue(resultWithValues.Result, out del))
			{
				if (args.Length == 0)
				{
					del = Expression.Lambda(resultWithValues.Result.Body, expression.Parameters).Compile();
				}
				else if (resultWithValues.Values.Length == 0)
				{
					del = Expression.Lambda(resultWithValues.Result.Body, resultWithValues.Result.AdditionalParameters).Compile();
				}
				else
				{
					del = Expression.Lambda(resultWithValues.Result.Body, expression.Parameters.Concat(resultWithValues.Result.AdditionalParameters)).Compile();
				}

				delegatesByCachedCompileResult = delegatesByCachedCompileResult.Clone(resultWithValues.Result, del, "delegatesByCachedCompileResult");
			}

			if (args.Length == 0)
			{
				return del.DynamicInvoke(resultWithValues.Values);
			}
			else if (resultWithValues.Values.Length == 0)
			{
				return del.DynamicInvoke(args);
			}
			else
			{
				return del.DynamicInvoke(args.Concat(resultWithValues.Values).ToArray());
			}
		}

		public static object CompileAndRun(Expression expression)
		{
			Delegate del;
			var resultWithValues = SubstituteConstants(expression);

			if ((del = resultWithValues.Result.compiledSimpleVersion) == null)
			{
				del = Expression.Lambda(resultWithValues.Result.Body, resultWithValues.Result.AdditionalParameters).Compile();

				resultWithValues.Result.compiledSimpleVersion = del;
			}

			return del.DynamicInvoke(resultWithValues.Values);
		}

		public static SubstituteConstantsResultWithValues SubstituteConstants(Expression expression)
		{
			object[] args = null;
			SubstituteConstantsResult result;
			
			if (!cachedSubstitutedExpressions.TryGetValue(expression, out result))
			{
				var values = new List<object>();
				var parameters = new List<ParameterExpression>();

				var replacement = SqlExpressionReplacer.Replace(expression, c =>
				{
					if (c.NodeType != ExpressionType.Constant)
					{
						return null;
					}

					var constantExpression = (ConstantExpression)c;

					values.Add(constantExpression.Value);
					var parameter = Expression.Parameter(constantExpression.Type);
					parameters.Add(parameter);

					return parameter;
				});

				args = values.ToArray();
				var parametersArray = parameters.ToArray();

				result = new SubstituteConstantsResult(replacement, parametersArray);

				cachedSubstitutedExpressions = cachedSubstitutedExpressions.Clone(expression, result, "cachedSubstitutedExpressions");
			}

			if (args == null)
			{
				var i = 0;
				args = new object[result.AdditionalParameters.Length];

				SqlExpressionReplacer.Replace(expression, c =>
				{
					if (c.NodeType != ExpressionType.Constant)
					{
						return null;
					}

					var constantExpression = (ConstantExpression)c;

					args[i++] = constantExpression.Value;

					return null;
				});
			}

			return new SubstituteConstantsResultWithValues(result, args);
		}
	}
}
