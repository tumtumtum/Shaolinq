// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

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
			case SqlFeature.SupportsAndPrefersInlineForeignKeysWherePossible:
				return false;
			case SqlFeature.Deferrability:
				return true;
			default:
				return false;
			}
		}
		
		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			if (symbol == SqlSyntaxSymbol.AutoIncrement)
			{
				return "AUTOINCREMENT";
			}

			return base.GetSyntaxSymbolString(symbol);
		}
	}
}
