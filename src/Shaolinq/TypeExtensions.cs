// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Platform;

namespace Shaolinq
{
	internal static class TypeExtensions
	{
		public static bool IsIntegralType(this Type type)
		{
			type = type.GetUnwrappedNullableType();

			return type == typeof(string)
				   || type.IsNumericType()
				   || type == typeof(Guid)
				   || type == typeof(DateTime)
				   || type == typeof(TimeSpan)
				   || type == typeof(bool);
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
