using Platform;

namespace Shaolinq.Persistence.Sql.Postgres.Shared
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

		public override bool SupportsIndexNameCasing
		{
			get
			{
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

		public override bool SupportsForUpdate
		{
			get
			{
				return true;
			}
		}

		public override bool SupportsIndexToLower
		{
			get
			{
				return true;
			}
		}
	}
}
