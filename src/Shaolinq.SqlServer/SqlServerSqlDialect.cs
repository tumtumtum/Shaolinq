// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Text.RegularExpressions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDialect
		: SqlDialect
	{
		private readonly bool marsEnabled;
		
		internal SqlServerSqlDialect(SqlServerSqlDatabaseContextInfo contextInfo)
		{
			if (contextInfo != null)
			{
				var connectionString = contextInfo.ConnectionString;
				var marsEnabledInConnectionString = connectionString != null && Regex.IsMatch(connectionString, @".*MultipleActiveResultSets\s*=\s*true.*", RegexOptions.IgnoreCase);

				marsEnabled = contextInfo.MultipleActiveResultSets || marsEnabledInConnectionString;
			}
        }

		public override bool SupportsCapability(SqlCapability capability)
		{
			switch (capability)
			{
			case SqlCapability.InsertOutput:
			case SqlCapability.PragmaIdentityInsert:
				return true;
			case SqlCapability.MultipleActiveResultSets:
				return marsEnabled;
			case SqlCapability.Deferrability:
			case SqlCapability.CascadeAction:
			case SqlCapability.DeleteAction:
			case SqlCapability.SetDefaultAction:
			case SqlCapability.UpdateAutoIncrementColumns:
			case SqlCapability.SetNullAction:
				return false;
			default:
				return base.SupportsCapability(capability);
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
			case SqlSyntaxSymbol.AutoIncrement:
				return "IDENTITY";
			default:
				return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
