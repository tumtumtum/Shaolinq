// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection.Emit;
using Platform;

namespace Shaolinq.TypeBuilding
{
	public static class ILGeneratorExtensions
	{
		public static void EmitValue(this ILGenerator generator, Type type, object value)
		{
			if (value == null || (type.IsValueType && object.Equals(value, type.GetDefaultValue())))
			{
				generator.EmitDefaultValue(type);

				return;
			}

			LocalBuilder variable = null;
			var nullableType = Nullable.GetUnderlyingType(type);

			if (nullableType != null)
			{
				variable = generator.DeclareLocal(type);

				generator.Emit(OpCodes.Ldloca, variable);

				type = nullableType;
			}

			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				generator.Emit(OpCodes.Ldc_I4, ((bool)value) ? 1 : 0);
				break;
			case TypeCode.Char:
				generator.Emit(OpCodes.Ldc_I4, (char)value);
				break;
			case TypeCode.SByte:
				generator.Emit(OpCodes.Ldc_I4, (sbyte)value);
				break;
			case TypeCode.Byte:
				generator.Emit(OpCodes.Ldc_I4, (byte)value);
				break;
			case TypeCode.Int16:
				generator.Emit(OpCodes.Ldc_I4, (int)(short)value);
				break;
			case TypeCode.UInt16:
				generator.Emit(OpCodes.Ldc_I4, (ushort)value);
				break;
			case TypeCode.Int32:
				generator.Emit(OpCodes.Ldc_I4, (int)value);
				break;
			case TypeCode.UInt32:
				generator.Emit(OpCodes.Ldc_I4, unchecked((int)(uint)value));
				break;
			case TypeCode.Int64:
				generator.Emit(OpCodes.Ldc_I8, (long)value);
				break;
			case TypeCode.UInt64:
				generator.Emit(OpCodes.Ldc_I8, unchecked((long)(ulong)value));
				break;
			case TypeCode.Single:
				generator.Emit(OpCodes.Ldc_R4, (float)value);
				break;
			case TypeCode.Double:
				generator.Emit(OpCodes.Ldc_R8, (double)value);
				break;
			case TypeCode.Decimal:
				var local = generator.DeclareLocal(typeof(int[]));
				var bits = decimal.GetBits((decimal)value);

				generator.Emit(OpCodes.Ldc_I4, bits.Length);
				generator.Emit(OpCodes.Newarr);
				generator.Emit(OpCodes.Stloc, local);

				for (var i = 0; i < bits.Length; i++)
				{
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Ldc_I4, i);
					generator.Emit(OpCodes.Ldc_I4, bits[i]);
					generator.Emit(OpCodes.Stelem_I4);
				}

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Call, TypeUtils.GetConstructor(() => new decimal(default(int[]))));
				break;
			case TypeCode.DateTime:
				generator.Emit(OpCodes.Ldc_I8, ((DateTime)value).Ticks);
				generator.Emit(OpCodes.Call, TypeUtils.GetConstructor(() => new DateTime(default(long))));
				break;
			case TypeCode.String:
				generator.Emit(OpCodes.Ldstr, (string)value);
				break;
			default:
				if (type == typeof(TimeSpan))
				{
					generator.Emit(OpCodes.Ldc_I8, ((TimeSpan)value).Ticks);
					generator.Emit(OpCodes.Call, TypeUtils.GetConstructor(() => new TimeSpan(default(long))));
				}
				else
				{
					throw new InvalidOperationException($"Unsupported type {type.Name} & value {value}");
				}
				break;
			}

			if (variable != null)
			{
				generator.Emit(OpCodes.Call, typeof(Nullable<>).MakeGenericType(type).GetConstructor(new[] { type }));
				generator.Emit(OpCodes.Ldloc, variable);
			}
		}

		public static void EmitDefaultValue(this ILGenerator generator, Type type)
		{
			if (!type.IsValueType)
			{
				generator.Emit(OpCodes.Ldnull);

				return;
			}

			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
				generator.Emit(OpCodes.Ldc_I4_0);
				break;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				generator.Emit(OpCodes.Ldc_I8, 0L);
				break;
			case TypeCode.Single:
				generator.Emit(OpCodes.Ldc_R4, (float)0);
				break;
			case TypeCode.Double:
				generator.Emit(OpCodes.Ldc_R8, (double)0);
				break;
			default:
				if (type == typeof(Guid))
				{
					generator.Emit(OpCodes.Ldsfld, FieldInfoFastRef.GuidEmptyGuid);
				}
				else
				{
					var local = generator.DeclareLocal(type);

					generator.Emit(OpCodes.Ldloca, local);
					generator.Emit(OpCodes.Initobj, local.LocalType);
					generator.Emit(OpCodes.Ldloc, local);
				}
				break;
			}
		}
	}
}
