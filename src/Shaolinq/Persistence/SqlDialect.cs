// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence
{
	public class SqlDialect
	{
		public virtual bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.AlterTableAddConstraints:
			case SqlCapability.Constraints:
			case SqlCapability.IndexNameCasing:
			case SqlCapability.IndexToLower:
			case SqlCapability.Deferrability:
			case SqlCapability.InsertIntoReturning:
			case SqlCapability.ForeignKeys:
			case SqlCapability.CascadeAction:
			case SqlCapability.DeleteAction:
			case SqlCapability.SetNullAction:
			case SqlCapability.SetDefaultAction:
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
			case SqlSyntaxSymbol.StringQuoteAlt:
				return "\"";
			case SqlSyntaxSymbol.StringEscape:
				return "\\";
			default:
				return "";
			}
		}
	}
}
