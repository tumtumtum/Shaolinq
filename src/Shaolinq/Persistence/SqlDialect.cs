// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;

namespace Shaolinq.Persistence
{
	public class SqlDialect
	{
		public static readonly SqlDialect Default = new SqlDialect();

		protected SqlDialect()
		{
		}

		public virtual bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
			case SqlFeature.AlterTableAddConstraints:
			case SqlFeature.Constraints:
			case SqlFeature.IndexNameCasing:
			case SqlFeature.IndexToLower:
			case SqlFeature.SelectForUpdate:
			case SqlFeature.Deferrability:
			case SqlFeature.InsertIntoReturning:
			case SqlFeature.ForeignKeys:
			case SqlFeature.CascadeAction:
			case SqlFeature.DeleteAction:
			case SqlFeature.SetNullAction:
			case SqlFeature.SetDefaultAction:
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
