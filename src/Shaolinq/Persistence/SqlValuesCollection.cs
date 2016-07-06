// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence
{
	public class SqlValuesEnumerable
		: IEnumerable
	{
		private static Dictionary<Type, Func<object, int>> getCountFuncs = new Dictionary<Type, Func<object, int>>();

		private readonly IEnumerable inner;

		public SqlValuesEnumerable(IEnumerable inner)
		{
			this.inner = inner;
		}

		public bool IsEmpty()
		{
			var value = this.inner;

			if (value == null)
			{
				return true;
			}

			var type = value.GetType();
			PropertyInfo countProperty;

			if (type.IsArray)
			{
				return (((Array)value).Length == 0);
			}
			else if ((countProperty = type.GetProperty("Count")) != null)
			{
				Func<object, int> counter = null;

				if (!getCountFuncs.TryGetValue(type, out counter))
				{
					var param = Expression.Parameter(typeof(object));
					var body = Expression.Property(Expression.Convert(param, type), countProperty);

					counter = (Func<object, int>)Expression.Lambda(body, param).Compile();

					getCountFuncs = getCountFuncs.Clone(type, counter);
				}

				var count = counter?.Invoke(value);

				return count == 0;
			}
			else 
			{
				var enumerator = value.GetEnumerator();

				while (enumerator.MoveNext())
				{
					return false;
				}

				return true;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.inner.GetEnumerator();
	}
}
