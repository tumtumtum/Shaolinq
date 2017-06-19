using System;
using System.ComponentModel;

namespace Shaolinq.Persistence
{
	public static class TypeConverterExtensions
	{
		public static object ConvertToFix(this TypeConverter typeConverter, object value, Type destinationType)
		{
			if (value == null && destinationType != typeof(string))
			{
				return null;
			}

			return typeConverter.ConvertTo(value, destinationType);
		}
	}
}