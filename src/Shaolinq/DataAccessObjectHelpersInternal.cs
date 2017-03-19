// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using Platform;

namespace Shaolinq
{
	public static class DataAccessObjectHelpersInternal
	{
		public static P AddToCollection<P, C>(P parent, Func<P, RelatedDataAccessObjects<C>> getChildren, C child, int version)
			where P : DataAccessObject
			where C : DataAccessObject
		{
			if (parent == null)
			{
				return null;
			}

			getChildren(parent).Add(child, version);

			return parent;
		}

		public static T Include<T, U>(this T obj, Expression<Func<T, U>> include)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return obj;
			// ReSharper restore SuspiciousTypeConversion.Global
		}

		public static Expression GetPropertyValueExpressionFromPredicatedDeflatedObject<T, U>(T obj, string propertyPath)
			where T : DataAccessObject
		{
			var pathComponents = propertyPath.Split('.');
			var parameter = Expression.Parameter(typeof(T));

			var propertyExpression = pathComponents.Aggregate<string, Expression>
			(
				parameter,
				(instance, name) => Expression.Property(instance, instance.Type.GetMostDerivedProperty(name))
			);

			var expression = obj.dataAccessModel.GetDataAccessObjects<T>()
				.Where((Expression<Func<T, bool>>)obj.ToObjectInternal().DeflatedPredicate)
				.Select(Expression.Lambda<Func<T, U>>(propertyExpression, parameter)).Expression;

			return Expression.Call(null, TypeUtils.GetMethod(() => default(IQueryable<U>).First()), expression);
		}
	}
}