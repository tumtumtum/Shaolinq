// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	internal class PostgresSqlDialect
		: SqlDialect
	{
		public override bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.IndexToLower:
			case SqlCapability.Deferrability:
			case SqlCapability.SelectForUpdate:
			case SqlCapability.CrossApply:
			case SqlCapability.OuterApply:
				return true;
			case SqlCapability.InlineForeignKeys:
				return false;
			case SqlCapability.MultipleActiveResultSets:
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
