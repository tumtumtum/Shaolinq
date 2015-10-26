using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;

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

			if (value.GetType().IsArray)
			{
				return (((Array)value).Length == 0);
			}
			else if (typeof(ICollection<>).IsAssignableFromIgnoreGenericParameters(value.GetType()))
			{
				Func<object, int> counter = null;

				if (!getCountFuncs.TryGetValue(value.GetType(), out counter))
				{

					var type = value.GetType().WalkHierarchy(true, false).FirstOrDefault(c => c.Name == typeof(ICollection<>).Name);

					if (type != null)
					{
						var prop = type.GetProperty("Count");

						var param = Expression.Parameter(typeof(object), "value");
						Expression body = Expression.Convert(param, value.GetType());
						body = Expression.Property(body, prop);
						counter = (Func<object, int>)Expression.Lambda(body, param).Compile();

						getCountFuncs = new Dictionary<Type, Func<object, int>>(getCountFuncs) { [value.GetType()] = counter };
					}
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
