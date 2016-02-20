// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	internal class PostgresDotConnectSqlDialect
		: PostgresSqlDialect
	{
		public override bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.MultipleActiveResultSets:
				return true;
			default:
				return base.SupportsCapability(capability);
			}
		}
    }
}
