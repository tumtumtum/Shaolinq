// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Platform;

namespace Shaolinq.Persistence
{
	public class SqlDialect
	{
		public static readonly SqlDialect Default = new SqlDialect();

		public virtual bool SupportsFeature(SqlFeature feature)
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
					return true;
				case SqlFeature.SelectForUpdate:
					return true;
				default:
					return false;
			}
		}

		public virtual string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
				case SqlSyntaxSymbol.Null:
					return "NULL";
				case SqlSyntaxSymbol.Like:
					return "LIKE";
				case SqlSyntaxSymbol.IdentifierQuote:
					return "\"";
				case SqlSyntaxSymbol.ParameterPrefix:
					return "@";
				case SqlSyntaxSymbol.StringQuote:
					return "'";
				default:
					return "";
			}
		}
	}
}
