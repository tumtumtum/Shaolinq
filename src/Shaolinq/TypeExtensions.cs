// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Platform;
using Platform.Reflection;

namespace Shaolinq
{
	internal static class TypeExtensions
	{
		internal static Type JoinedObjectType(this Type type)
		{
			if (type.GetGenericTypeDefinitionOrNull() == typeof(RelatedDataAccessObjects<>))
			{
				return type.GetSequenceElementType();
			}
			else if (type.IsDataAccessObjectType())
			{
				return type;
			}

			throw new InvalidOperationException();
		}

		internal static bool CanBeNull(this Type type)
		{
			return !type.IsValueType || type.IsNullableType();
		}

		internal static bool IsTypeRequiringJoin(this Type type)
		{
			return type.IsDataAccessObjectType() || (type.GetSequenceElementType()?.IsDataAccessObjectType() ?? false);
		}

		public static bool IsExpressionTree(this Type type)
		{
			return typeof(Expression<>).IsAssignableFromIgnoreGenericParameters(type);
		}

		public static bool IsQueryable(this Type type)
		{
			return typeof(IQueryable<>).IsAssignableFromIgnoreGenericParameters(type);
		}

		public static bool IsIntegralType(this Type type)
		{
			type = type.GetUnwrappedNullableType();

			return type == typeof(string)
				   || type.IsNumericType()
				   || type == typeof(Guid)
				   || type == typeof(DateTime)
				   || type == typeof(TimeSpan)
				   || type == typeof(bool)
				   || type == typeof(byte[]);
		}
		
		internal static bool MemberIsConversionMember(this MemberInfo memberInfo)
		{
			var returnType = memberInfo.GetMemberReturnType();

			if (memberInfo is MethodInfo)
			{
				return memberInfo.Name == "GetValue" || memberInfo.Name == string.Concat("Get", returnType.Name, "Value") || returnType.Name == "ToValue" || memberInfo.Name == string.Concat("To", returnType.Name) || memberInfo.Name == string.Concat("To", returnType.Name, "Value");
			}

			if (memberInfo is PropertyInfo)
			{
				return memberInfo.Name == "Value" || memberInfo.Name == string.Concat(returnType.Name, "Value") || memberInfo.Name == returnType.Name;
			}

			return false;
		}

		internal static IEnumerable<MemberInfo> GetConversionMembers(this Type type, Type targetType = null)
		{
			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(c => targetType == null || c.ReturnType == targetType)
				.Where(c => c.Name == "op_Implicit"))
			{
				yield return method;
			}

			foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(c => targetType == null || c.GetMemberReturnType() == targetType)
				.Where(MemberIsConversionMember))
			{
				yield return member;
			}
		}

		internal static bool IsAssignableFromIncludingConversion(this Type type, Type other)
		{
			if (type.IsAssignableFrom(other))
			{
				return true;
			}

			return type.GetConversionMembers(other).Any();
		}

		public static NewExpression CreateNewExpression(this Type type, params Expression[] arguments)
		{
			if (arguments.Length == 0)
			{
				return Expression.New(type);
			}
			else
			{
				var constructor = type.GetConstructor(arguments.Select(c => c.Type).ToArray());

				if (constructor == null)
				{
					throw new InvalidOperationException($"Cannot find constructor for type {type}");
				}
				
				return Expression.New(constructor, arguments);
			}
		}

		public static bool IsNullableType(this Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static bool IsDataAccessObjectType(this Type type)
		{
			return typeof(DataAccessObject).IsAssignableFrom(type);
		}

		public static string ToHumanReadableName(this Type type)
		{
			var builder = new StringBuilder();

			type.AppendHumanReadableName(builder);

			return builder.ToString();
		}

		private static void AppendHumanReadableName(this Type type, StringBuilder builder)
		{
			if (type.IsGenericType)
			{
				builder.Append(type.Name.Remove(type.Name.LastIndexOf('`')));

				builder.Append("<");

				var i = 0;
				var genericArgs = type.GetGenericArguments();

				foreach (var innerType in genericArgs)
				{
					innerType.AppendHumanReadableName(builder);

					if (i != genericArgs.Length - 1)
					{
						builder.Append(", ");
					}
					i++;
				}

				builder.Append(">");
			}
			else
			{
				builder.Append(type.Name);
			}
		}
	}
}
