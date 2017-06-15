using System;
using System.ComponentModel;
using System.Globalization;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.Tests
{
	[TypeConverter(typeof(FixedDateTypeConverter))]
	public struct FixedDate
	{
		private readonly DateTime value;

		public FixedDate(DateTime value)
		{
			this.value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}

		public DateTime ToDateTime()
		{
			return this.value;
		}

		public static bool operator==(FixedDate left, FixedDate right)
		{
			return left.Equals(right);
		}

		public static bool operator!=(FixedDate left, FixedDate right)
		{
			return !left.Equals(right);
		}

		public override bool Equals(object obj)
		{
			return (obj as FixedDate?)?.Equals(this) ?? false;
		}

		public bool Equals(FixedDate other)
		{
			return this.value.Equals(other.value);
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public static implicit operator DateTime(FixedDate value)
		{
			return value.value;
		}

		public static implicit operator DateTime?(FixedDate value)
		{
			return value.value;
		}

		public static implicit operator DateTime? (FixedDate? value)
		{
			return value?.value;
		}

		public static implicit operator FixedDate(DateTime value)
		{
			return new FixedDate(value);
		}
		
		public static implicit operator FixedDate?(DateTime value)
		{
			return value == null ? null : (FixedDate?)new FixedDate(value);
		}

		public static implicit operator FixedDate? (DateTime? value)
		{
			return value == null ? null : (FixedDate?)new FixedDate(value.Value);
		}
	}

	public class SqlFixedDateDataType : UniversalTimeNormalisingDateTimeSqlDateType
	{
		public SqlFixedDateDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, bool nullable, SqlDataTypeProvider sqlDataTypeProvider)
			: base(constraintDefaultsConfiguration, nullable ? typeof(FixedDate?) : typeof(FixedDate), sqlDataTypeProvider.GetSqlDataType(typeof(DateTime)).GetSqlName(null))
		{
		}
	}

	public class FixedDateTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType.GetUnwrappedNullableType() == typeof(DateTime))
			{
				return true;
			}

			if (sourceType == typeof(object))
			{
				return true;
			}

			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType.GetUnwrappedNullableType() == typeof(DateTime))
			{
				return true;
			}

			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value == null && !destinationType.IsValueType)
			{
				return null;
			}

			if (destinationType == typeof(DateTime))
			{
				return (value as FixedDate?)?.ToDateTime();
			}
			else if (destinationType == typeof(DateTime?))
			{
				return (value as FixedDate?)?.ToDateTime();
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
			{
				return null;
			}

			if (value is DateTime)
			{
				return new FixedDate((DateTime)value);
			}

			if (value is string)
			{
				return new FixedDate(DateTime.Parse((string)value));
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
