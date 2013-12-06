// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence.Sql
{
	public class SqlDialect
	{
		public static readonly SqlDialect Default = new SqlDialect();

		public virtual char NameQuoteChar
		{
			get
			{
				return '`';
			}
		}

		public virtual string DeferrableText
		{
			get
			{
				return "";
			}
		}

		public virtual string LikeString
		{
			get
			{
				return "LIKE";
			}
		}

		public virtual bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
				case SqlFeature.AlterTableAddConstraints:
					return true;
				case SqlFeature.Constraints:
					return true;
				case SqlFeature.IndexNameCasing:
					return true;
				case SqlFeature.IndexToLower:
					return true;
				case SqlFeature.SelectForUpdate:
					return true;
				default:
					return false;
			}
		}

		public virtual string GetColumnName(PropertyDescriptor propertyDescriptor, SqlDataType sqlDataType, bool foreignKey)
		{
			return sqlDataType.GetSqlName(propertyDescriptor);
		}

		public virtual string GetAutoIncrementSuffix()
		{
			return "AUTO_INCREMENT";
		}
	}
}
