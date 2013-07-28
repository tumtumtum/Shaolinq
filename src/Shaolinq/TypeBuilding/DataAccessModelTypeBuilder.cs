using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Platform;
using Platform.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessModelTypeBuilder
		: BaseTypeBuilder
	{
		private TypeBuilder typeBuilder;
		private FieldBuilder dictionaryFieldBuilder;

		public DataAccessModelTypeBuilder(AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder)
			: base(assemblyBuildContext, moduleBuilder)
		{
		}

		public virtual Type BuildType(Type baseType)
		{
			typeBuilder = this.ModuleBuilder.DefineType(baseType.FullName, TypeAttributes.Class | TypeAttributes.Public, baseType);
			
			// Build constructor for DataAccessModel
			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
			var ctorGenerator = constructorBuilder.GetILGenerator();
			ctorGenerator.Emit(OpCodes.Ldarg_0);
			ctorGenerator.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
			ctorGenerator.Emit(OpCodes.Ret);

			var methodInfo = typeBuilder.BaseType.GetMethod("Initialise", BindingFlags.Instance | BindingFlags.NonPublic);
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			methodAttributes |= methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family);

			var initialiseMethodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, Type.EmptyTypes);
			var initialiseGenerator = initialiseMethodBuilder.GetILGenerator();
			
			dictionaryFieldBuilder = typeBuilder.DefineField("m$dataAccessObjectsByType", typeof(Dictionary<Type,IQueryable>), FieldAttributes.Private);

			initialiseGenerator.Emit(OpCodes.Ldarg_0);
			initialiseGenerator.Emit(OpCodes.Ldc_I4, baseType.GetProperties().Count());
			initialiseGenerator.Emit(OpCodes.Newobj, dictionaryFieldBuilder.FieldType.GetConstructor(new Type[] { typeof(int) }));
			initialiseGenerator.Emit(OpCodes.Stfld, dictionaryFieldBuilder);

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
				
				// Use the persistance context attribute on the property if it exists
				var propertyPersistenceContextAttribute = propertyInfo.GetFirstCustomAttribute<PersistenceContextAttribute>(true);

				Type type = null;
				Type t = propertyInfo.PropertyType;

				while (t != null)
				{
					if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DataAccessObjectsQueryable<>))
					{
						type = t.GetGenericArguments()[0];
					}

					t = t.BaseType;
				}

				var persistenceContextAttribute = propertyPersistenceContextAttribute ?? type.GetFirstCustomAttribute<PersistenceContextAttribute>(true) ?? PersistenceContextAttribute.Default;

				if (propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetSetMethod().IsAbstract)
				{
					// Generate the field for the queryable
					var fieldBuilder = typeBuilder.DefineField("m$" + propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Private);

					// Create new queryable and assign to field
					initialiseGenerator.Emit(OpCodes.Ldarg_0);
					initialiseGenerator.Emit(OpCodes.Newobj, propertyInfo.PropertyType.GetConstructor(new Type[0]));
					initialiseGenerator.Emit(OpCodes.Stfld, fieldBuilder);
					
					// Call DataAccessObjectsQueryable.Initialize
					initialiseGenerator.Emit(OpCodes.Ldarg_0);
					initialiseGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
					initialiseGenerator.Emit(OpCodes.Ldarg_0);
					initialiseGenerator.Emit(OpCodes.Ldnull);
					initialiseGenerator.Emit(OpCodes.Callvirt, propertyInfo.PropertyType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(BaseDataAccessModel), typeof(Expression) }, null));

					// Add to dictionary
					initialiseGenerator.Emit(OpCodes.Ldarg_0);
					initialiseGenerator.Emit(OpCodes.Ldfld, dictionaryFieldBuilder);
					initialiseGenerator.Emit(OpCodes.Ldtoken, propertyInfo.PropertyType);
					initialiseGenerator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
					initialiseGenerator.Emit(OpCodes.Ldarg_0);
					initialiseGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
					initialiseGenerator.Emit(OpCodes.Callvirt, dictionaryFieldBuilder.FieldType.GetMethod("set_Item"));
                    
					var propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, Type.EmptyTypes);

					// Implement get method

					if (propertyInfo.GetGetMethod() != null && propertyInfo.GetGetMethod().IsAbstract)
					{
						propertyBuilder.SetGetMethod(BuildPropertyMethod("get", propertyInfo, fieldBuilder));
					}

					// Implement set method

					if (propertyInfo.GetSetMethod() != null && propertyInfo.GetSetMethod().IsAbstract)
					{
						throw new InvalidOperationException(string.Format("The property '{0}.{1}' should not have a setter because it is a [DataAccessObjects] property", baseType.Name, propertyInfo.Name));
					}
				}
			}

			initialiseGenerator.Emit(OpCodes.Ret);

			BuildGetDataAccessObjectsMethod();
         
			return typeBuilder.CreateType();
		}

		protected virtual void BuildGetDataAccessObjectsMethod()
		{
			var method = typeBuilder.BaseType.GetMethod("GetDataAccessObjects");
			var methodAttributes = (method.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot)) | MethodAttributes.Virtual;
			var methodBuilder = this.typeBuilder.DefineMethod(method.Name, methodAttributes, method.CallingConvention, method.ReturnType, method.GetParameters().Select(c => c.ParameterType).ToArray());
			var genericParameters = methodBuilder.DefineGenericParameters(new string[] { "T" });

			genericParameters[0].SetInterfaceConstraints(typeof(IDataAccessObject));

			var generator = methodBuilder.GetILGenerator();

			var label = generator.DefineLabel();
			var local = generator.DeclareLocal(typeof(IQueryable));
			var typedLocal = generator.DeclareLocal(methodBuilder.ReturnType);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dictionaryFieldBuilder);
			generator.Emit(OpCodes.Ldtoken, genericParameters[0]);
			generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
			generator.Emit(OpCodes.Ldloca, local);
			generator.Emit(OpCodes.Callvirt, typeof(Dictionary<Type, IQueryable>).GetMethod("TryGetValue", new Type[] { typeof(Type), typeof(IQueryable).MakeByRefType() }));
			generator.Emit(OpCodes.Brfalse, label);
			
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Castclass, methodBuilder.ReturnType);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(label);

			var constructorPrep = typeof(DataAccessObjects<>).GetConstructor(new Type[0]);
			var constructor = TypeBuilder.GetConstructor(typeof(DataAccessObjects<>).MakeGenericType(genericParameters[0]), constructorPrep);

			// Create new queryable
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Newobj, constructor);
			generator.Emit(OpCodes.Stloc, typedLocal);

			// Call Initialize method
			generator.Emit(OpCodes.Ldloc, typedLocal);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldnull);

			var methodPrep = typeof(DataAccessObjectsQueryable<>).GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(BaseDataAccessModel), typeof(Expression) }, null);
			var initializeMethod =TypeBuilder.GetMethod(typeof(DataAccessObjectsQueryable<>).MakeGenericType(genericParameters[0]), methodPrep);

			generator.Emit(OpCodes.Callvirt, initializeMethod);

			// Store in dictionary
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dictionaryFieldBuilder);
			generator.Emit(OpCodes.Ldtoken, genericParameters[0]);
			generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
			generator.Emit(OpCodes.Ldloc, typedLocal);
			generator.Emit(OpCodes.Callvirt, dictionaryFieldBuilder.FieldType.GetMethod("set_Item"));

			generator.Emit(OpCodes.Ldloc, typedLocal);
			generator.Emit(OpCodes.Castclass, methodBuilder.ReturnType);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual MethodBuilder BuildPropertyMethod(string methodType, PropertyInfo propertyInfo, FieldBuilder backingField)
		{
			MethodInfo method;
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

			var parameters = method.GetParameters().Convert(x => x.ParameterType).ToArray();
			var methodBuilder = this.typeBuilder.DefineMethod(method.Name, methodAttributes, method.CallingConvention, method.ReturnType, parameters);
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
