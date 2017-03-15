// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using Platform;

namespace Shaolinq
{
	public class DataAccessObjectHelpersInternal
	{
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