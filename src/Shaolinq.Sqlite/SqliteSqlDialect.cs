// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDialect
		: SqlDialect
	{
		public new static readonly SqliteSqlDialect Default = new SqliteSqlDialect();

		public override bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
				case SqlFeature.AlterTableAddConstraints:
					return false;
				case SqlFeature.Constraints:
					return true;
				case SqlFeature.IndexNameCasing:
					return true;
				case SqlFeature.IndexToLower:
					return true;
				case SqlFeature.SelectForUpdate:
					return false;
				default:
					return false;
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
