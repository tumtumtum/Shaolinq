// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.MySql
{
	public class MySqlSqlDialect
		: SqlDialect
	{
		public new static readonly MySqlSqlDialect Default = new MySqlSqlDialect();

		public override string GetAutoIncrementSuffix()
		{
			return "AUTO_INCREMENT";
		}

		public override bool SupportsForUpdate
		{
			get
			{
				return true;
			}
		}
	}
}
