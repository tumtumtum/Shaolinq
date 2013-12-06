// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSqlDialect
		: SqlDialect
	{
		public new static readonly PostgresSqlDialect Default = new PostgresSqlDialect();

		public override char NameQuoteChar
		{
			get
			{
				return '\"';
			}
		}

		public override string LikeString
		{
			get
			{
				return "ILIKE";
			}
		}

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

		public override string GetColumnName(PropertyDescriptor propertyDescriptor, SqlDataType sqlDataType, bool isForiegnKey)
		{
			var type = sqlDataType.UnderlyingType ?? sqlDataType.SupportedType;

			if (!isForiegnKey && propertyDescriptor.IsAutoIncrement && type.IsIntegerType())
			{
				return "SERIAL";
			}
			else
			{
				return base.GetColumnName(propertyDescriptor, sqlDataType, isForiegnKey);
			}
		}

		public override string GetAutoIncrementSuffix()
		{
			return "";
		}
	}
}
