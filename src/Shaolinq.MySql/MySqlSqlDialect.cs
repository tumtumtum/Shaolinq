// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDialect
		: SqlDialect
	{
		public new static readonly MySqlSqlDialect Default = new MySqlSqlDialect();

		private MySqlSqlDialect()
		{	
		}

		public override bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
			case SqlFeature.Deferrability:
				return false;
			default:
				return base.SupportsFeature(feature);
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
				case SqlSyntaxSymbol.IdentifierQuote:
					return "`";
				case SqlSyntaxSymbol.ParameterPrefix:
					return "?";
				case SqlSyntaxSymbol.AutoIncrement:
					return "AUTO_INCREMENT";
				default:
					return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
