// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shaolinq.TypeBuilding
{
	public class AssemblyBuildContext
	{
		public Assembly TargetAssembly { get; }
		public ModuleBuilder ModuleBuilder { get; }
		public TypeBuilder DataAccessModelPropertiesTypeBuilder { get; }
		public TypeBuilder DataAccessModelTypeBuilder { get; set; }
		public FieldInfo DataAccessModelPropertiesTypeBuilderField { get; set; }
		public Dictionary<Type, DataAccessObjectTypeBuilder> TypeBuilders { get; }
		public TypeBuilder ConstantsContainer { get; }
		public Dictionary<object, FieldBuilder> FieldsByValue { get; }

		public AssemblyBuildContext(Assembly targetAssembly, ModuleBuilder moduleBuilder, TypeBuilder dataAccessModelPropertiesTypeBuilder)
		{
			this.TargetAssembly = targetAssembly;
			this.ModuleBuilder = moduleBuilder;
			this.DataAccessModelPropertiesTypeBuilder = dataAccessModelPropertiesTypeBuilder;
			this.TypeBuilders = new Dictionary<Type, DataAccessObjectTypeBuilder>();
			this.FieldsByValue = new Dictionary<object, FieldBuilder>();
			this.ConstantsContainer = this.ModuleBuilder.DefineType(targetAssembly.GetName().Name + "__CONSTANTS__");	
		}

		public FieldBuilder GetFieldByConstant<T>(T value, string prefix = "__value")
		{
			FieldBuilder fieldBuilder;

			if (!this.FieldsByValue.TryGetValue(value, out fieldBuilder))
			{
				fieldBuilder = this.ConstantsContainer.DefineField(prefix + "_" +this.FieldsByValue.Count, typeof(T), FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly);

				this.FieldsByValue[value] = fieldBuilder;
			}

			return fieldBuilder;
		}

		public void FinishConstantsContainer()
		{
			var construcor = this.ConstantsContainer.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

			var generator = construcor.GetILGenerator();

			foreach (var fieldByValue in this.FieldsByValue)
			{
				switch (Type.GetTypeCode(fieldByValue.Key.GetType()))
				{
				case TypeCode.Int16:
					generator.Emit(OpCodes.Ldc_I4, (short)fieldByValue.Key);
					generator.Emit(OpCodes.Stsfld, fieldByValue.Value);
					break;
				case TypeCode.Int32:
					generator.Emit(OpCodes.Ldc_I4, (int)fieldByValue.Key);
					generator.Emit(OpCodes.Stsfld, fieldByValue.Value);
					break;
				case TypeCode.Int64:
					generator.Emit(OpCodes.Ldc_I8, (long)fieldByValue.Key);
					generator.Emit(OpCodes.Stsfld, fieldByValue.Value);
					break;
				}
			}

			generator.Emit(OpCodes.Ret);
		}
	}
}
