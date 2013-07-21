using System;

namespace Shaolinq.Persistence.Sql
{
	public abstract class SqlDataTypeProvider
	{
		public abstract SqlDataType GetSqlDataType(Type type);
	}
}
