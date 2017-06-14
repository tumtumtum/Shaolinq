using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using PropertyDescriptor = Shaolinq.Persistence.PropertyDescriptor;

namespace Shaolinq.Tests
{
	[TypeConverter(typeof(DateTypeConverter))]
	public struct Date
	{
		private readonly DateTime value;

		public Date(DateTime value)
		{
			this.value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}

		public DateTime ToDateTime()
		{
			return this.value;
		}
	}

	public class SqlDateDataType : UniversalTimeNormalisingDateTimeSqlDateType
	{
		public SqlDateDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, bool nullable)
			: base(constraintDefaultsConfiguration, nullable ? typeof(Date?) : typeof(Date), "DATETIME2")
		{
		}
	}

	public class DateTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(DateTime))
			{
				return true;
			}

			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(DateTime))
			{
				return true;
			}

			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(DateTime))
			{
				return ((Date)value).ToDateTime();
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is DateTime)
			{
				return new Date((DateTime)value);
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
