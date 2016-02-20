// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Platform;

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
