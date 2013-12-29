// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDialect
		: SqlDialect
	{
		public new static readonly PostgresSharedSqlDialect Default = new PostgresSharedSqlDialect();

		public override string DeferrableText
		{
			get
			{
				return "DEFERRABLE INITIALLY DEFERRED";
			}
		}

		public override bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
				case SqlFeature.AlterTableAddConstraints:
					return true;
				case SqlFeature.Constraints:
					return true;
				case SqlFeature.IndexNameCasing:
					return false;
				case SqlFeature.IndexToLower:
					return true;
				case SqlFeature.SelectForUpdate:
					return true;
				default:
					return false;
			}
		}

		public override string GetColumnDataTypeName(PropertyDescriptor propertyDescriptor, SqlDataType sqlDataType, bool isForiegnKey)
		{
			var type = sqlDataType.UnderlyingType ?? sqlDataType.SupportedType;

			if (!isForiegnKey && propertyDescriptor.IsAutoIncrement && type.IsIntegerType())
			{
				return "SERIAL";
			}
			else
			{
				return base.GetColumnDataTypeName(propertyDescriptor, sqlDataType, isForiegnKey);
			}
		}

		public override string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
				case SqlSyntaxSymbol.Like:
					return "ILIKE";
				case SqlSyntaxSymbol.AutoIncrementSuffix:
					return "";
				default:
					return base.GetSyntaxSymbolString(symbol);
			}
		}
	}
}
