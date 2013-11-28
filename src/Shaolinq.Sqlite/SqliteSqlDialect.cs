// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDialect
		: SqlDialect
	{
		public new static readonly SqliteSqlDialect Default = new SqliteSqlDialect();

		public override bool SupportsConstraints
		{
			get
			{
				return true;
			}
		}

		public override string DeferrableText
		{
			get
			{
				return "DEFERRABLE INITIALLY DEFERRED";
			}
		}

		public override string GetAutoIncrementSuffix()
		{
			return "AUTOINCREMENT";
		}
	}
}
