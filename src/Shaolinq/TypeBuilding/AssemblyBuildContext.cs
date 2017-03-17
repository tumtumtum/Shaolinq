// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Platform;

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
		public Dictionary<string, FieldBuilder> FieldsByValue { get; }

		public AssemblyBuildContext(Assembly targetAssembly, ModuleBuilder moduleBuilder, TypeBuilder dataAccessModelPropertiesTypeBuilder)
		{
			this.TargetAssembly = targetAssembly;
			this.ModuleBuilder = moduleBuilder;
			this.DataAccessModelPropertiesTypeBuilder = dataAccessModelPropertiesTypeBuilder;
			this.TypeBuilders = new Dictionary<Type, DataAccessObjectTypeBuilder>();
			this.FieldsByValue = new Dictionary<string, FieldBuilder>();
			this.ConstantsContainer = this.ModuleBuilder.DefineType("___STRINGHASHCODES___");	
		}

		public FieldBuilder GetHashCodeField(string value, string prefix = "__hash")
		{
			FieldBuilder fieldBuilder;

			if (!this.FieldsByValue.TryGetValue(value, out fieldBuilder))
			{
				fieldBuilder = this.ConstantsContainer.DefineField(prefix + "_" +this.FieldsByValue.Count, typeof(int), FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly);

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
				generator.Emit(OpCodes.Ldstr, fieldByValue.Key);
				generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<string>(c => c.GetHashCode()));
				generator.Emit(OpCodes.Stsfld, fieldByValue.Value);
			}

			generator.Emit(OpCodes.Ret);
		}
	}
}
