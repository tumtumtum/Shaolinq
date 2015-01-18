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
			case SqlFeature.IndexToLower:
			case SqlFeature.Deferrability:
			case SqlFeature.SelectForUpdate:
			case SqlFeature.InlineForeignKeys:
				return true;
			default:
				return base.SupportsFeature(feature);
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
