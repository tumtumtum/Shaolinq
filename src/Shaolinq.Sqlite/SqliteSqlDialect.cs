// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDialect
		: SqlDialect
	{
		private static readonly SqlFeature[] Features =
		{
			SqlFeature.Constraints, 
			SqlFeature.IndexToLower, 
			SqlFeature.SelectForUpdate,
			SqlFeature.IndexToLower,
			SqlFeature.Deferrability
		};

		public new static readonly SqliteSqlDialect Default = new SqliteSqlDialect();

		public SqliteSqlDialect()
			: base(Features)
		{	
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
