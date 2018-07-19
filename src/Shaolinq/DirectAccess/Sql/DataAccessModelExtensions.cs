using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq.DirectAccess.Sql
{
	public static partial class DataAccessModelExtensions
	{
		[RewriteAsync]
		public static IDbConnection OpenConnection<T>(this T model, bool forWrite = true)
			where T : DataAccessModel
		{
			return model.GetCurrentDataContext(forWrite).SqlDatabaseContext.OpenConnection();
		}
	}
}
