using System;
using System.Reflection.Emit;

namespace Shaolinq.TypeBuilding
{
	public static class ILGeneratorExtensions
	{
		public static void EmitDefaultValue(this ILGenerator generator, Type type)
		{
			if (type.IsValueType)
			{
				if (type == typeof(short))
				{
					generator.Emit(OpCodes.Ldc_I4_0);
				}
				else if (type == typeof(int))
				{
					generator.Emit(OpCodes.Ldc_I4_0);
				}
				else if (type == typeof(long))
				{
					generator.Emit(OpCodes.Ldc_I8, 0L);
				}
				else if (type == typeof(Guid))
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
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}
		}
	}
}
