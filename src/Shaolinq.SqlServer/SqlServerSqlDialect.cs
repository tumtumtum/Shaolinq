using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDialect
		: SqlDialect
	{
		public new static readonly SqlServerSqlDialect Default;

		static SqlServerSqlDialect()
		{
			Default = new SqlServerSqlDialect(SqlFeature.AlterTableAddConstraints, SqlFeature.Constraints, SqlFeature.IndexNameCasing, SqlFeature.SelectForUpdate, SqlFeature.InsertOutput);
		}

		private SqlServerSqlDialect(params SqlFeature[] supportedFeatures)
			: base(supportedFeatures)
		{	
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
			case SqlSyntaxSymbol.AutoIncrement:
				return "IDENTITY(1,1)";
			default:
				return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
