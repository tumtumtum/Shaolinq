// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	internal class DataAccessModelTypeBuilder
		: BaseTypeBuilder
	{
		private FieldBuilder dictionaryFieldBuilder;

		public DataAccessModelTypeBuilder(AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder)
			: base(assemblyBuildContext, moduleBuilder)
		{
		}

		public virtual void BuildTypePhase1(Type baseType)
		{
			this.AssemblyBuildContext.DataAccessModelTypeBuilder = this.ModuleBuilder.DefineType(baseType.FullName, TypeAttributes.Class | TypeAttributes.Public, baseType);
			this.AssemblyBuildContext.DataAccessModelPropertiesTypeBuilderField = this.AssemblyBuildContext.DataAccessModelTypeBuilder.DefineField("$$$dataAccessModelProperties", this.AssemblyBuildContext.DataAccessModelPropertiesTypeBuilder, FieldAttributes.Public);
		}

		public virtual Type BuildTypePhase2()
		{
			var typeBuilder = this.AssemblyBuildContext.DataAccessModelTypeBuilder;
			var baseType = this.AssemblyBuildContext.DataAccessModelTypeBuilder.BaseType;

			// Build constructor for DataAccessModel
			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
			var ctorGenerator = constructorBuilder.GetILGenerator();
			ctorGenerator.Emit(OpCodes.Ldarg_0);
			ctorGenerator.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));
			ctorGenerator.Emit(OpCodes.Ldarg_0);
			ctorGenerator.Emit(OpCodes.Newobj, this.AssemblyBuildContext.DataAccessModelPropertiesTypeBuilderField.FieldType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null));
			ctorGenerator.Emit(OpCodes.Stfld, this.AssemblyBuildContext.DataAccessModelPropertiesTypeBuilderField);
			ctorGenerator.Emit(OpCodes.Ret);

			var methodInfo = typeBuilder.BaseType.GetMethod("Initialise", BindingFlags.Instance | BindingFlags.NonPublic);
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			methodAttributes |= methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family);

			var initialiseMethodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, Type.EmptyTypes);
			var generator = initialiseMethodBuilder.GetILGenerator();

			this.dictionaryFieldBuilder = typeBuilder.DefineField("$$$dataAccessObjectsByType", typeof(Dictionary<Type,IQueryable>), FieldAttributes.Private);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4, baseType.GetProperties().Count());
			generator.Emit(OpCodes.Newobj, this.dictionaryFieldBuilder.FieldType.GetConstructor(new Type[] { typeof(int) }));
			generator.Emit(OpCodes.Stfld, this.dictionaryFieldBuilder);

			foreach (var propertyInfo in baseType.GetProperties())
			{
				var queryableAttribute = propertyInfo.GetFirstCustomAttribute<DataAccessObjectsAttribute>(true);

				if (queryableAttribute == null)
				{
					continue;
				}

				if (typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(propertyInfo.PropertyType))
				{
					throw new InvalidOperationException("DataAccessModel objects should not defined properties of type RelatedDataAccessObject<>");
				}

				// Generate the field for the queryable
				var fieldBuilder = typeBuilder.DefineField("$$" + propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Private);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldnull);
				generator.Emit(OpCodes.Newobj, propertyInfo.PropertyType.GetConstructor(new[] { typeof(DataAccessModel), typeof(Expression) }));
				generator.Emit(OpCodes.Stfld, fieldBuilder);

				// Add to dictionary
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dictionaryFieldBuilder);
				generator.Emit(OpCodes.Ldtoken, propertyInfo.PropertyType.GetGenericArguments()[0]);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandleMethod);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, fieldBuilder);
				generator.Emit(OpCodes.Callvirt, this.dictionaryFieldBuilder.FieldType.GetMethod("set_Item"));

				var propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, Type.EmptyTypes);

				// Implement get method

				if (propertyInfo.GetGetMethod() != null)
				{
					propertyBuilder.SetGetMethod(this.BuildPropertyMethod("get", propertyInfo, fieldBuilder));
				}

				// Implement set method

				if (propertyInfo.GetSetMethod() != null)
				{
					propertyBuilder.SetSetMethod(BuildRelatedDataAccessObjectsSetMethod(propertyInfo));
				}
			}

			foreach (var item in this.AssemblyBuildContext.propertyDescriptors.Keys.OrderBy(c => c.Item2))
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<DataAccessModel>(c => c.TypeDescriptorProvider).GetGetMethod());
				generator.Emit(OpCodes.Ldtoken, item.Item1);
				generator.Emit(OpCodes.Call, TypeUtils.GetMethod(() => Type.GetTypeFromHandle(default(RuntimeTypeHandle))));
				generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<TypeDescriptorProvider>(c => c.GetTypeDescriptor(default(Type))));
				generator.Emit(OpCodes.Ldstr, item.Item2);
				generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<TypeDescriptor>(c => c.GetPropertyDescriptorByPropertyName(default(string))));
				generator.Emit(OpCodes.Stfld, this.AssemblyBuildContext.propertyDescriptors[item]);
			}

			generator.Emit(OpCodes.Ret);

			this.BuildGetDataAccessObjectsMethod();
			
			return typeBuilder.CreateType();
		}

		private MethodBuilder BuildRelatedDataAccessObjectsSetMethod(PropertyInfo propertyInfo)
		{
			var typeBuilder = this.AssemblyBuildContext.DataAccessModelTypeBuilder;

			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (propertyInfo.GetSetMethod().Attributes  & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var method = typeBuilder.DefineMethod("set_" + propertyInfo.Name, methodAttributes, typeof(void), new Type[] { propertyInfo.PropertyType });

			var generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldsfld, $"You cannot explicit set the property {this.AssemblyBuildContext.DataAccessModelTypeBuilder.Name}.'{propertyInfo.Name}'");

			generator.Emit(OpCodes.Newobj, TypeUtils.GetConstructor(() => new NotImplementedException(default(string))));

			generator.Emit(OpCodes.Throw);
			generator.Emit(OpCodes.Ret);

			return method;
		}

		protected virtual void BuildGetDataAccessObjectsMethod()
		{
			var typeBuilder = this.AssemblyBuildContext.DataAccessModelTypeBuilder;
			var method = TypeUtils.GetMethod<DataAccessModel>(c => c.GetDataAccessObjects(default(Type)));
			var methodAttributes = (method.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot)) | MethodAttributes.Virtual;
			var methodBuilder = typeBuilder.DefineMethod(method.Name, methodAttributes, method.CallingConvention, method.ReturnType, method.GetParameters().Select(c => c.ParameterType).ToArray());
			
			var generator = methodBuilder.GetILGenerator();

			var lockObj = generator.DeclareLocal(typeof(object));
			var acquiredLock = generator.DeclareLocal(typeof(bool));
			var label = generator.DefineLabel();
			var local = generator.DeclareLocal(typeof(IQueryable));

			var returnLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, acquiredLock);
			
			generator.BeginExceptionBlock();

			var b = false;

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dictionaryFieldBuilder);
			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Stloc, lockObj);
			generator.Emit(OpCodes.Ldloca, acquiredLock);
			generator.Emit(OpCodes.Call, TypeUtils.GetMethod(() => Monitor.Enter(default(object), ref b)));
			
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dictionaryFieldBuilder);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldloca, local);
			generator.Emit(OpCodes.Callvirt, typeof(Dictionary<Type, IQueryable>).GetMethod("TryGetValue", new [] { typeof(Type), typeof(IQueryable).MakeByRefType() }));
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Nop);
			generator.Emit(OpCodes.Leave, returnLabel);
			
			generator.MarkLabel(label);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, typeof(IDataAccessModelInternal));
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessModelInternal>(c => c.CreateDataAccessObjects(default(Type))));
			generator.Emit(OpCodes.Stloc, local);
			
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dictionaryFieldBuilder);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Callvirt, typeof(Dictionary<Type, IQueryable>).GetMethod("set_Item"));
			generator.Emit(OpCodes.Nop);
			generator.Emit(OpCodes.Nop);

			generator.BeginFinallyBlock();
			var boolValue = generator.DeclareLocal(typeof(bool));
			generator.Emit(OpCodes.Ldloc, acquiredLock);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Stloc, boolValue);
			generator.Emit(OpCodes.Ldloc, boolValue);
			var endOfFinally = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue, endOfFinally);
			generator.Emit(OpCodes.Ldloc, lockObj);
			generator.Emit(OpCodes.Call, TypeUtils.GetMethod(() => Monitor.Exit(default(object))));
			generator.Emit(OpCodes.Nop);
			generator.MarkLabel(endOfFinally);
			generator.EndExceptionBlock();
			
			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual MethodBuilder BuildPropertyMethod(string methodType, PropertyInfo propertyInfo, FieldBuilder backingField)
		{
			MethodInfo method;
			var typeBuilder = this.AssemblyBuildContext.DataAccessModelTypeBuilder;
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			methodAttributes |= (propertyInfo.GetGetMethod().Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
		
			switch (methodType)
			{
				case "get":
					method = propertyInfo.GetGetMethod();
					break;
				case "set":
					method = propertyInfo.GetSetMethod();
					break;
				default:
					throw new NotSupportedException(methodType);
			}

			var parameters = method.GetParameters().Select(x => x.ParameterType).ToArray();
			var methodBuilder = typeBuilder.DefineMethod(method.Name, methodAttributes, method.CallingConvention, method.ReturnType, parameters);
			var generator = methodBuilder.GetILGenerator();

			switch (methodType)
			{
				case "get":
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, backingField);
					generator.Emit(OpCodes.Ret);
					break;
				case "set":
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Stfld, backingField);
					generator.Emit(OpCodes.Ret);
					break;
			}

			return methodBuilder;
		}
	}
}
