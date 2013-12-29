// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDialect
		: SqlDialect
	{
		public new static readonly MySqlSqlDialect Default = new MySqlSqlDialect();

		public override bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
				case SqlFeature.AlterTableAddConstraints:
					return true;
				case SqlFeature.Constraints:
					return true;
				case SqlFeature.IndexNameCasing:
					return true;
				case SqlFeature.IndexToLower:
					return false;
				case SqlFeature.SelectForUpdate:
					return true;
				default:
					return false;
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			if (symbol == SqlSyntaxSymbol.IdentifierQuote)
			{
				return "`";
			}

			return base.GetSyntaxSymbolString(symbol);
		}
	}
}
