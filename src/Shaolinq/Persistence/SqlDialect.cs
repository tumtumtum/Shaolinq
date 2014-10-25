// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;

namespace Shaolinq.Persistence
{
	public class SqlDialect
	{
		public static readonly SqlDialect Default;

		static SqlDialect()
		{
			Default = new SqlDialect
			(
				new []
				{
					SqlFeature.AlterTableAddConstraints,
					SqlFeature.Constraints,
					SqlFeature.IndexNameCasing,
					SqlFeature.IndexToLower,
					SqlFeature.SelectForUpdate,
					SqlFeature.Deferrability,
					SqlFeature.InsertIntoReturning,
					SqlFeature.SupportsInlineForeignKeys
				}
			);
		}

		private readonly SqlFeature[] supportedFeatures;
		
		public SqlDialect(params SqlFeature[] supportedFeatures)
		{
			this.supportedFeatures = supportedFeatures;
		}

		public SqlFeature[] GetAllSupportedFeatures()
		{
			return (SqlFeature[])this.supportedFeatures.Clone();
		}

		public virtual bool SupportsFeature(SqlFeature feature)
		{
			return this.supportedFeatures.Contains(feature);
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
