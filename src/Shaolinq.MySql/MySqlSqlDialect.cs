// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlSqlDialect
		: SqlDialect
	{
		public new static readonly MySqlSqlDialect Default;

		static MySqlSqlDialect()
		{
			Default = new MySqlSqlDialect
			(
				new []
				{
					SqlFeature.AlterTableAddConstraints,
					SqlFeature.Constraints,
					SqlFeature.IndexNameCasing,
					SqlFeature.SelectForUpdate
				}
			);
		}

		private MySqlSqlDialect(params SqlFeature[] supportedFeatures)
			: base(supportedFeatures)
		{	
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
