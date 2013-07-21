using System;

namespace Shaolinq.Persistence.Sql
{
	public class SqlDatabaseCreationException
		: Exception
	{
		public SqlDatabaseCreationException(string message)
			: base(message)
		{
		}
	}
}
