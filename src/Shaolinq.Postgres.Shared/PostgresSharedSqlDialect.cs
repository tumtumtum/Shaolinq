// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDialect
		: SqlDialect
	{
		public new static readonly PostgresSharedSqlDialect Default;

		static PostgresSharedSqlDialect()
		{
			Default = new PostgresSharedSqlDialect
			(
				new []
				{
					SqlFeature.AlterTableAddConstraints, 
					SqlFeature.Constraints, 
					SqlFeature.IndexToLower, 
					SqlFeature.SelectForUpdate,
					SqlFeature.Deferrability
				}
			);
		}

		private PostgresSharedSqlDialect(params SqlFeature[] supportedFeatures)
			: base(supportedFeatures)
		{	
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
