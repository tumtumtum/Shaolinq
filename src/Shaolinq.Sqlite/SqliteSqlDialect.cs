// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDialect
		: SqlDialect
	{
		public override bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.SelectForUpdate:
			case SqlCapability.AlterTableAddConstraints:
				return false;
			case SqlCapability.Constraints:
			case SqlCapability.IndexToLower:
			case SqlCapability.Deferrability:
			case SqlCapability.InlineForeignKeys:
			case SqlCapability.MultipleActiveResultSets:
				return true;
			default:
				return base.SupportsCapability(capability);
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
			case SqlSyntaxSymbol.AutoIncrement:
				return "AUTOINCREMENT";
			case SqlSyntaxSymbol.StringQuote:
				return "'";
			case SqlSyntaxSymbol.StringEscape:
				return "'";
			default:
				return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
