// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq.Expressions
{
	internal class BinaryOperations
	{
		public static Func<object, object, object> GetAddFunc(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.String:
				return AddString;
			case TypeCode.Decimal:
				return AddDecimal;
			case TypeCode.Double:
				return AddDouble;
			case TypeCode.Single:
				return AddFloat;
			case TypeCode.UInt64:
				return AddULong;
			case TypeCode.Int64:
				return AddLong;
			case TypeCode.UInt32:
				return AddUInt;
			case TypeCode.Int32:
				return AddInt;
			case TypeCode.UInt16:
				return AddUShort;
			case TypeCode.Int16:
				return AddShort;
			case TypeCode.Char:
				return AddChar;
			case TypeCode.SByte:
				return AddSByte;
			case TypeCode.Byte:
				return AddByte;
			}

			throw new InvalidOperationException($"Unsupported type {type}");
		}

		public static Func<object, object, object> GetSubtractFunc(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Decimal:
				return SubtractDecimal;
			case TypeCode.Double:
				return SubtractDouble;
			case TypeCode.Single:
				return SubtractFloat;
			case TypeCode.UInt64:
				return SubtractULong;
			case TypeCode.Int64:
				return SubtractLong;
			case TypeCode.UInt32:
				return SubtractUInt;
			case TypeCode.Int32:
				return SubtractInt;
			case TypeCode.UInt16:
				return SubtractUShort;
			case TypeCode.Int16:
				return SubtractShort;
			case TypeCode.Char:
				return SubtractChar;
			case TypeCode.SByte:
				return SubtractSByte;
			case TypeCode.Byte:
				return SubtractByte;
			}

			throw new InvalidOperationException($"Unsupported type {type}");
		}

		public static Func<object, object, object> GetMultiplyFunc(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Decimal:
				return MultiplyDecimal;
			case TypeCode.Double:
				return MultiplyDouble;
			case TypeCode.Single:
				return MultiplyFloat;
			case TypeCode.UInt64:
				return MultiplyULong;
			case TypeCode.Int64:
				return MultiplyLong;
			case TypeCode.UInt32:
				return MultiplyUInt;
			case TypeCode.Int32:
				return MultiplyInt;
			case TypeCode.UInt16:
				return MultiplyUShort;
			case TypeCode.Int16:
				return MultiplyShort;
			case TypeCode.Char:
				return MultiplyChar;
			case TypeCode.SByte:
				return MultiplySByte;
			case TypeCode.Byte:
				return MultiplyByte;
			}

			throw new InvalidOperationException($"Unsupported type {type}");
		}

		public static Func<object, object, object> GetDivideFunc(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Decimal:
				return DivideDecimal;
			case TypeCode.Double:
				return DivideDouble;
			case TypeCode.Single:
				return DivideFloat;
			case TypeCode.UInt64:
				return DivideULong;
			case TypeCode.Int64:
				return DivideLong;
			case TypeCode.UInt32:
				return DivideUInt;
			case TypeCode.Int32:
				return DivideInt;
			case TypeCode.UInt16:
				return DivideUShort;
			case TypeCode.Int16:
				return DivideShort;
			case TypeCode.Char:
				return DivideChar;
			case TypeCode.SByte:
				return DivideSByte;
			case TypeCode.Byte:
				return DivideByte;
			}

			throw new InvalidOperationException($"Unsupported type {type}");
		}

		// Add

		public static object AddString(object left, object right)
		{
			return (string)left + (string)right;
		}

		public static object AddDecimal(object left, object right)
		{
			return (decimal)left + (decimal)right;
		}

		public static object AddDouble(object left, object right)
		{
			return (double)left + (double)right;
		}

		public static object AddFloat(object left, object right)
		{
			return (float)left + (float)right;
		}

		public static object AddLong(object left, object right)
		{
			return (long)left + (long)right;
		}

		public static object AddULong(object left, object right)
		{
			return (ulong)left + (ulong)right;
		}

		public static object AddInt(object left, object right)
		{
			return (int)left + (int)right;
		}

		public static object AddUInt(object left, object right)
		{
			return (uint)left + (uint)right;
		}

		public static object AddShort(object left, object right)
		{
			return (short)left + (short)right;
		}

		public static object AddUShort(object left, object right)
		{
			return (ushort)left + (ushort)right;
		}

		public static object AddChar(object left, object right)
		{
			return (char)left + (char)right;
		}

		public static object AddSByte(object left, object right)
		{
			return (sbyte)left + (sbyte)right;
		}

		public static object AddByte(object left, object right)
		{
			return (byte)left + (byte)right;
		}

		// Subtract
		
		public static object SubtractDecimal(object left, object right)
		{
			return (decimal)left - (decimal)right;
		}

		public static object SubtractDouble(object left, object right)
		{
			return (double)left - (double)right;
		}

		public static object SubtractFloat(object left, object right)
		{
			return (float)left - (float)right;
		}

		public static object SubtractLong(object left, object right)
		{
			return (long)left - (long)right;
		}

		public static object SubtractULong(object left, object right)
		{
			return (ulong)left - (ulong)right;
		}

		public static object SubtractInt(object left, object right)
		{
			return (int)left - (int)right;
		}

		public static object SubtractUInt(object left, object right)
		{
			return (uint)left - (uint)right;
		}

		public static object SubtractShort(object left, object right)
		{
			return (short)left - (short)right;
		}

		public static object SubtractUShort(object left, object right)
		{
			return (ushort)left - (ushort)right;
		}

		public static object SubtractChar(object left, object right)
		{
			return (char)left - (char)right;
		}

		public static object SubtractSByte(object left, object right)
		{
			return (sbyte)left - (sbyte)right;
		}

		public static object SubtractByte(object left, object right)
		{
			return (byte)left - (byte)right;
		}

		// Multiply

		public static object MultiplyDecimal(object left, object right)
		{
			return (decimal)left * (decimal)right;
		}

		public static object MultiplyDouble(object left, object right)
		{
			return (double)left * (double)right;
		}

		public static object MultiplyFloat(object left, object right)
		{
			return (float)left * (float)right;
		}

		public static object MultiplyLong(object left, object right)
		{
			return (long)left * (long)right;
		}

		public static object MultiplyULong(object left, object right)
		{
			return (ulong)left * (ulong)right;
		}

		public static object MultiplyInt(object left, object right)
		{
			return (int)left * (int)right;
		}

		public static object MultiplyUInt(object left, object right)
		{
			return (uint)left * (uint)right;
		}

		public static object MultiplyShort(object left, object right)
		{
			return (short)left * (short)right;
		}

		public static object MultiplyUShort(object left, object right)
		{
			return (ushort)left * (ushort)right;
		}

		public static object MultiplyChar(object left, object right)
		{
			return (char)left * (char)right;
		}

		public static object MultiplySByte(object left, object right)
		{
			return (sbyte)left * (sbyte)right;
		}

		public static object MultiplyByte(object left, object right)
		{
			return (byte)left * (byte)right;
		}

		// Divide

		public static object DivideDecimal(object left, object right)
		{
			return (decimal)left / (decimal)right;
		}

		public static object DivideDouble(object left, object right)
		{
			return (double)left / (double)right;
		}

		public static object DivideFloat(object left, object right)
		{
			return (float)left / (float)right;
		}

		public static object DivideLong(object left, object right)
		{
			return (long)left / (long)right;
		}

		public static object DivideULong(object left, object right)
		{
			return (ulong)left / (ulong)right;
		}

		public static object DivideInt(object left, object right)
		{
			return (int)left / (int)right;
		}

		public static object DivideUInt(object left, object right)
		{
			return (uint)left / (uint)right;
		}

		public static object DivideShort(object left, object right)
		{
			return (short)left / (short)right;
		}

		public static object DivideUShort(object left, object right)
		{
			return (ushort)left / (ushort)right;
		}

		public static object DivideChar(object left, object right)
		{
			return (char)left / (char)right;
		}

		public static object DivideSByte(object left, object right)
		{
			return (sbyte)left / (sbyte)right;
		}

		public static object DivideByte(object left, object right)
		{
			return (byte)left / (byte)right;
		}
	}
}