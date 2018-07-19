using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq.DirectAccess.Sql
{
	public static class DataAccessScopeExtensions
	{
		public static IDbCommand CreateCommand<T>(this T model)
			where T : DataAccessModel
		{
			return model.GetCurrentCommandsContext().CreateCommand();
		}

		public static IAsyncEnumerable<T> ExecuteReaderAsync<T>(this T model, string sql, params object[] arguments)
			where T : DataAccessModel
		{
			throw new NotSupportedException();

			//return model.GetCurrentCommandsContext().ExecuteReader(sql, new List<TypedValue>(arguments.Select(c => new TypedValue(c?.GetType() ?? typeof(object), c))).ToReadOnlyCollection());
		}
	}
}
