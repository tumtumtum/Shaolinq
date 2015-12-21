// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	internal class PostgresSqlDialect
		: SqlDialect
	{
		public new static readonly PostgresSqlDialect Default = new PostgresSqlDialect();

		private PostgresSqlDialect()
		{	
		}

		public override bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.IndexToLower:
			case SqlCapability.Deferrability:
			case SqlCapability.SelectForUpdate:
				return true;
			case SqlCapability.InlineForeignKeys:
				return false;
			default:
				return base.SupportsCapability(capability);
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
