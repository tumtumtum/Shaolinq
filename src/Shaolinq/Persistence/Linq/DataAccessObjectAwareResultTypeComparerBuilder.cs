using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence.Linq
{
	internal class DataAccessObjectAwareResultTypeComparerBuilder
	{
		private static readonly MethodInfo needsComparerMethod = TypeUtils.GetMethod(() => NeedsComparer<object>()).GetGenericMethodDefinition();
		private static readonly MethodInfo createComparerMethod = TypeUtils.GetMethod(() => CreateComparer<object>()).GetGenericMethodDefinition();

		private static class ComparerContainer<T>
		{
			public static bool needed;
			public static bool created;
			public static volatile Func<T, T, bool> comparer;
		}

		public static Func<T, T, bool> CreateComparer<T>()
		{
			if (!ComparerContainer<T>.created)
			{
				var expression = CreateComparerLambdaExpression<T>();
				
				ComparerContainer<T>.needed = !(expression.Body.NodeType == ExpressionType.Constant && (bool)((ConstantExpression)expression.Body).Value == false);
				ComparerContainer<T>.comparer = expression.Compile();
				ComparerContainer<T>.created = true;
			}

			return ComparerContainer<T>.comparer;
		}

		private static Dictionary<RuntimeTypeHandle, Func<bool>> needsComparerFuncs = new Dictionary<RuntimeTypeHandle, Func<bool>>();

		public static bool NeedsComparer(Type type)
		{
			Func<bool> func;

			if (!needsComparerFuncs.TryGetValue(type.TypeHandle, out func))
			{
				func = Expression.Lambda<Func<bool>>(Expression.Call(needsComparerMethod.MakeGenericMethod(type))).Compile();

				needsComparerFuncs = needsComparerFuncs.Clone(type.TypeHandle, func);
			}

			return func();
		}
		
		public static bool NeedsComparer<T>()
		{
			if (IsConsideredSimpleType(typeof(T)))
			{
				return false;
			}
			var type = typeof(T);

			if (typeof(T).Assembly == typeof(DataAccessObject).Assembly && !(typeof(T).IsArray || (type.GetInterfaces().Any(c => c.GetGenericTypeDefinitionOrNull() == typeof(IEnumerable<>) && c.GetGenericArguments()[0].IsDataAccessObjectType()))))
			{
				return false;
			}

			if (!ComparerContainer<T>.created)
			{
				CreateComparer<T>();
			}

			return ComparerContainer<T>.needed;
		}

		public static Expression<Func<T, T, bool>> CreateComparerLambdaExpression<T>()
		{
			var foundDataAccessObject = false;

			return (Expression<Func<T, T, bool>>)CreateComparerLambdaExpression(typeof(T), ref foundDataAccessObject);
		}

		private static readonly HashSet<Type> currentlyBuildingTypes = new HashSet<Type>();

		public static LambdaExpression CreateComparerLambdaExpression(Type type, ref bool foundDataAccessObject)
		{
			var leftParam = Expression.Parameter(type);
			var rightParam = Expression.Parameter(type);

			if (currentlyBuildingTypes.Contains(type))
			{
				return Expression.Lambda(Expression.Invoke(Expression.Call(createComparerMethod.MakeGenericMethod(type)), leftParam, rightParam), leftParam, rightParam);
			}

			currentlyBuildingTypes.Add(type);

			try
			{
				var body = CreateComparerExpression(type, leftParam, rightParam, ref foundDataAccessObject);

				if (!foundDataAccessObject)
				{
					return Expression.Lambda(Expression.Constant(false), leftParam, rightParam);
				}

				return Expression.Lambda(body, leftParam, rightParam);
			}
			finally
			{
				currentlyBuildingTypes.Remove(type);
			}
		}

		private static bool IsConsideredSimpleType(Type type)
		{
			type = type.GetUnwrappedNullableType();

			return type.IsPrimitive || (type.IsIntegralType() && !type.IsArray) || type == typeof(Type);
		}

		private static bool ConsideredTypeForBasicComparison(Type type)
		{
			if (type.GetInterfaces().Any(c => c.GetGenericTypeDefinitionOrNull() == typeof(IEnumerable<>) && c.GetGenericArguments()[0].IsDataAccessObjectType()))
			{
				return false;
			}
			
			return type.IsPrimitive
				|| type.IsIntegralType() 
				|| type.IsArray 
				|| type == typeof(Type) || typeof(IEnumerable).IsAssignableFrom(type)
				|| ((type.Assembly == typeof(object).Assembly) && !(type.Name.StartsWith("Tuple") || type.Name.StartsWith("KeyValuePair")));
		}

		public static Expression CreateComparerExpression(Type type, Expression left, Expression right, ref bool foundDataAccessObject)
		{
			var originalType = type;
			type = type.GetUnwrappedNullableType();

			Expression body;

			if (ConsideredTypeForBasicComparison(type))
			{
				return Expression.Equal(left, right);
			}
			
			if (originalType.IsNullableType())
			{
				body = Expression.AndAlso(Expression.Equal(left, Expression.Constant(null, type)), Expression.Equal(right, Expression.Constant(null, type)));
				body = Expression.Or(body, CreateComparerExpression(type, left, right, ref foundDataAccessObject));

				return body;
			}

			body = Expression.Equal(left, right);

			Expression propertiesExpressions = null;

			if (type.IsArray && type.GetArrayRank() == 1 && type.GetElementType().IsDataAccessObjectType())
			{
				foundDataAccessObject = true;

				var elementType = type.GetElementType();

				body = Expression.OrElse
				(
					body,
					Expression.Call
					(
						TypeUtils.GetMethod(() => default(IEnumerable<string>).SequenceEqual(default(IEnumerable<string>), default(IEqualityComparer<string>)))
						.GetGenericMethodDefinition()
						.MakeGenericMethod(elementType),
						left,
						right,
						Expression.Constant(typeof(ObjectReferenceIdentityEqualityComparer<>).MakeGenericType(elementType).GetField("Default", BindingFlags.Static | BindingFlags.Public).GetValue(null))
					)
				);
			}
			else if (!type.IsDataAccessObjectType())
			{
				foreach (var member in type
					.GetProperties(BindingFlags.Instance | BindingFlags.Public)
					.Where(c => c.CanRead)
					.Cast<MemberInfo>()
					.Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(c => !c.IsInitOnly)))

				{
					Expression currentPropertyExpression;
					var currentLeft = Expression.MakeMemberAccess(left, member);
					var currentRight = Expression.MakeMemberAccess(right, member);

					if (ConsideredTypeForBasicComparison(member.GetMemberReturnType()))
					{
						currentPropertyExpression = CreateComparerExpression(member.GetMemberReturnType(), currentLeft, currentRight, ref foundDataAccessObject);
					}
					else
					{
						currentPropertyExpression = Expression.Invoke(CreateComparerLambdaExpression(member.GetMemberReturnType(), ref foundDataAccessObject), currentLeft, currentRight);
					}

					propertiesExpressions = propertiesExpressions == null ? currentPropertyExpression : Expression.AndAlso(propertiesExpressions, currentPropertyExpression);
				}
			}
			else if (type.IsDataAccessObjectType())
			{
				foundDataAccessObject = true;
			}

			return propertiesExpressions == null ? body : Expression.OrElse(body, propertiesExpressions);
		}
	}
}