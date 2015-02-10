// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDialect
		: SqlDialect
	{
		public new static readonly PostgresSharedSqlDialect Default = new PostgresSharedSqlDialect();

		private PostgresSharedSqlDialect()
		{	
		}

		public override bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
			case SqlFeature.IndexToLower:
			case SqlFeature.Deferrability:
			case SqlFeature.SelectForUpdate:
				return true;
			case SqlFeature.InlineForeignKeys:
				return false;
			default:
				return base.SupportsFeature(feature);
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
				case SqlSyntaxSymbol.Like:
					return "ILIKE";
				default:
					return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
