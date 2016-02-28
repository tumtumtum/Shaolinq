// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Computed;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;

// ReSharper disable UnusedMember.Local

namespace Shaolinq.TypeBuilding
{
	public sealed class DataAccessObjectTypeBuilder
	{
		internal const string ForceSetPrefix = "$$force_set";
		internal const string IsSetSuffix = "$$is_set";
		internal const string HasChangedSuffix = "$$changed";
		internal const string DataObjectFieldName = "$$data";

		private static readonly Regex BuildMethodRegex = new Regex("^Build(.*)(Method|Property)$", RegexOptions.Compiled);

		public ModuleBuilder ModuleBuilder { get; }
		public AssemblyBuildContext AssemblyBuildContext { get; }

		private readonly Type baseType;
		private TypeBuilder typeBuilder;
		private FieldInfo swappingField;
		private FieldInfo dataObjectField;
		private ILGenerator cctorGenerator;
		private FieldInfo originalPrimaryKeyField;
		private FieldInfo isDeflatedReferenceField;
		private FieldInfo finishedInitializingField;
		private FieldBuilder partialObjectStateField;
		private TypeBuilder dataObjectTypeTypeBuilder;
		private readonly TypeDescriptor typeDescriptor;
		private ConstructorBuilder dataConstructorBuilder;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		
		private readonly Dictionary<string, FieldBuilder> valueFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueIsSetFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueChangedFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> computedFuncFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, PropertyBuilder> propertyBuilders = new Dictionary<string, PropertyBuilder>();
		private readonly Dictionary<string, MethodBuilder> setComputedValueMethods = new Dictionary<string, MethodBuilder>();

		public struct TypeBuildContext
		{
			public int Passcount { get; set; }

			public TypeBuildContext(int passcount)
				: this()
			{
				this.Passcount = passcount;
			}

			public bool IsFirstPass()
			{
				return this.Passcount == 1;
			}

			public bool IsSecondPass()
			{
				return this.Passcount == 2;
			}
		}

		public DataAccessObjectTypeBuilder(TypeDescriptorProvider typeDescriptorProvider, AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder, Type baseType)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;
			this.baseType = baseType;
			this.ModuleBuilder = moduleBuilder;
			this.AssemblyBuildContext = assemblyBuildContext;

			assemblyBuildContext.TypeBuilders[baseType] = this;

			this.typeDescriptor = this.GetTypeDescriptor(baseType);
		}

		private TypeDescriptor GetTypeDescriptor(Type type)
		{
			return this.typeDescriptorProvider.GetTypeDescriptor(type);
		}
		
		public void Build(TypeBuildContext typeBuildContext)
		{
			if (typeBuildContext.IsFirstPass())
			{
				this.typeBuilder = this.ModuleBuilder.DefineType(this.baseType.FullName, TypeAttributes.Class | TypeAttributes.Public, this.baseType);
				this.dataObjectTypeTypeBuilder = this.ModuleBuilder.DefineType(this.baseType.FullName + "Data", TypeAttributes.Class | TypeAttributes.Public, typeof(object));
				this.typeBuilder.AddInterfaceImplementation(typeof(IDataAccessObjectAdvanced));
				this.typeBuilder.AddInterfaceImplementation(typeof(IDataAccessObjectInternal));

				this.originalPrimaryKeyField = this.typeBuilder.DefineField("originalPrimaryKeyFlattened", typeof(ObjectPropertyValue[]), FieldAttributes.Public);
				this.finishedInitializingField = this.dataObjectTypeTypeBuilder.DefineField("finishedInitializing", typeof(bool), FieldAttributes.Public);
				this.swappingField = this.dataObjectTypeTypeBuilder.DefineField("swappingField", typeof(bool), FieldAttributes.Public);

				// Static constructor

				var staticConstructor = this.typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

				this.cctorGenerator = staticConstructor.GetILGenerator();

				// Define default constructor

				var defaultConstructorBuilder = this.typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
				var defaultConstructorGenerator = defaultConstructorBuilder.GetILGenerator();
				defaultConstructorGenerator.Emit(OpCodes.Ldarg_0);
				defaultConstructorGenerator.Emit(OpCodes.Call, typeBuilder.BaseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, Type.EmptyTypes, null));
				defaultConstructorGenerator.Emit(OpCodes.Ret);

				// Define constructor for data object type

				this.dataConstructorBuilder = this.dataObjectTypeTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
				var constructorGenerator = this.dataConstructorBuilder.GetILGenerator();
				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));
				constructorGenerator.Emit(OpCodes.Ret);

				var attributeBuilder = new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

				this.dataObjectTypeTypeBuilder.SetCustomAttribute(attributeBuilder);
				this.partialObjectStateField = this.dataObjectTypeTypeBuilder.DefineField("PartialObjectState", typeof(ObjectState), FieldAttributes.Public);
				
				this.isDeflatedReferenceField = this.dataObjectTypeTypeBuilder.DefineField("IsDeflatedReference", typeof(bool), FieldAttributes.Public);

				this.dataObjectField = this.typeBuilder.DefineField(DataObjectFieldName, this.dataObjectTypeTypeBuilder, FieldAttributes.Public);
			}
        
			var type = this.baseType;
			var alreadyImplementedProperties = new HashSet<string>();

			while (type != null)
			{
				foreach (var propertyInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				{
					if (propertyInfo.DeclaringType != type || alreadyImplementedProperties.Contains(propertyInfo.Name))
					{
						break;
					}

					alreadyImplementedProperties.Add(propertyInfo.Name);

					var persistedMemberAttribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);
					var relatedObjectsAttribute = propertyInfo.GetFirstCustomAttribute<RelatedDataAccessObjectsAttribute>(true);
					var relatedObjectAttribute = propertyInfo.GetFirstCustomAttribute<BackReferenceAttribute>(true);

					if (persistedMemberAttribute != null && !propertyInfo.PropertyType.IsDataAccessObjectType())
					{
						var propertyDescriptor = this.GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						if (propertyInfo.GetGetMethod() == null)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is missing a get accessor", propertyInfo.Name, this.typeDescriptor.Type.Name);
						}

						if (propertyInfo.GetSetMethod() == null && !propertyDescriptor.IsComputedTextMember && !propertyDescriptor.IsComputedMember)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is missing a set accessor", propertyInfo.Name, this.typeDescriptor.Type.Name);
						}

						if ((propertyInfo.GetGetMethod().Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is not declared as virtual or abstract", propertyInfo.Name, this.typeDescriptor.Type.Name);
						}

						if (propertyInfo.GetSetMethod() != null && (propertyInfo.GetSetMethod().Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is not declared as virtual or abstract", propertyInfo.Name, this.typeDescriptor.Type.Name);
						}

						this.BuildPersistedProperty(propertyInfo, typeBuildContext);

						if (typeBuildContext.IsFirstPass())
						{
							if (propertyDescriptor.IsComputedTextMember)
							{
								this.BuildSetComputedTextPropertyMethod(propertyInfo, typeBuildContext);
							}
							else if (propertyDescriptor.IsComputedMember)
							{
								this.BuildSetComputedPropertyMethod(propertyInfo, typeBuildContext);
							}
						}
					}
					else if (persistedMemberAttribute != null && propertyInfo.PropertyType.IsDataAccessObjectType())
					{
						var propertyDescriptor = this.GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						this.BuildPersistedProperty(propertyInfo, typeBuildContext);
					}
					else if (relatedObjectAttribute != null)
					{
						var propertyDescriptor = this.GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						this.BuildPersistedProperty(propertyInfo, typeBuildContext);
					}
					else if (relatedObjectsAttribute != null)
					{
						this.BuildRelatedDataAccessObjectsProperty(propertyInfo, typeBuildContext);
					}
				}

				type = type.BaseType;
			}

			if (typeBuildContext.IsSecondPass())
			{
				type = this.baseType;

				while (type != null)
				{
					foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
					{
						if (propertyInfo.DeclaringType != type)
						{
							break;
						}

						var persistedMemberAttribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);

						if (persistedMemberAttribute != null && !propertyInfo.PropertyType.IsDataAccessObjectType())
						{
							var propertyDescriptor = this.GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

							if (propertyDescriptor.IsComputedTextMember)
							{
								this.BuildSetComputedTextPropertyMethod(propertyInfo, typeBuildContext);
							}
							else if (propertyDescriptor.IsComputedMember)
							{
								this.BuildSetComputedPropertyMethod(propertyInfo, typeBuildContext);
							}
						}
					}

					type = type.BaseType;
				}
			
				this.cctorGenerator.Emit(OpCodes.Ret);

				var constructorBuilder = this.typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(DataAccessModel), typeof(bool) });
				var constructorGenerator = constructorBuilder.GetILGenerator();

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Call, this.baseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Newobj, this.dataConstructorBuilder);
				constructorGenerator.Emit(OpCodes.Stfld, this.dataObjectField);

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Ldarg_1);
				constructorGenerator.Emit(OpCodes.Stfld, TypeUtils.GetField<DataAccessObject>(c => c.dataAccessModel));

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Ldarg_2);
				constructorGenerator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.SetIsNew(default(bool))));

				var skipSetDefault = constructorGenerator.DefineLabel();
				constructorGenerator.Emit(OpCodes.Ldarg_2);
				constructorGenerator.Emit(OpCodes.Brfalse, skipSetDefault);

				foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties)
				{
					if (propertyDescriptor.IsAutoIncrement
						&& propertyDescriptor.PropertyType.GetUnwrappedNullableType() == typeof(Guid))
					{
						var guidLocal = constructorGenerator.DeclareLocal(propertyDescriptor.PropertyType);
						
						if (propertyDescriptor.PropertyType.IsNullableType())
						{
							constructorGenerator.Emit(OpCodes.Ldloca, guidLocal);
							constructorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuidMethod);
							constructorGenerator.Emit(OpCodes.Call, TypeUtils.GetConstructor<Guid?>(() => new Guid?(Guid.Empty)));
						}
						else
						{
							constructorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuidMethod);
							constructorGenerator.Emit(OpCodes.Stloc, guidLocal);
						}

						constructorGenerator.Emit(OpCodes.Ldarg_0);
						constructorGenerator.Emit(OpCodes.Ldloc, guidLocal);
						constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyDescriptor.PropertyName].GetSetMethod());
					}
					else if (propertyDescriptor.PropertyType.IsValueType 
						&& Nullable.GetUnderlyingType(propertyDescriptor.PropertyType) == null 
						&& !propertyDescriptor.IsPrimaryKey
						&& !propertyDescriptor.IsAutoIncrement
						&& !propertyDescriptor.IsComputedMember)
					{
						constructorGenerator.Emit(OpCodes.Ldarg_0);
						constructorGenerator.EmitDefaultValue(propertyDescriptor.PropertyType);
						constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyDescriptor.PropertyName].GetSetMethod());
					}
					else if (propertyDescriptor.PropertyType == typeof(string)
						&& !propertyDescriptor.IsComputedMember
						&& !propertyDescriptor.IsComputedTextMember)
					{
						constructorGenerator.Emit(OpCodes.Ldarg_0);
						constructorGenerator.Emit(OpCodes.Ldnull);

						if (this.propertyBuilders.ContainsKey(ForceSetPrefix + propertyDescriptor.PropertyName))
						{
							constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[ForceSetPrefix + propertyDescriptor.PropertyName].GetSetMethod());
						}
						else
						{
							constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyDescriptor.PropertyName].GetSetMethod());
						}
					}
				}

				constructorGenerator.MarkLabel(skipSetDefault);
				constructorGenerator.Emit(OpCodes.Ret);

				this.dataObjectTypeTypeBuilder.CreateType();

				this.BuildAbstractMethods();

				this.typeBuilder.CreateType();
			}
		}

		private void BuildAbstractMethods()
		{
			foreach (var method in this
				.GetType()
				.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				.Where(c => BuildMethodRegex.IsMatch(c.Name))
				.Where(c => c.GetParameters().Length == 0))
			{
				method.Invoke(this, null);
			}
		}

		private void BuildPrimaryKeyIsCommitReadyProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			var returnTrueLabel = generator.DefineLabel();
			var returnFalseLabel = generator.DefineLabel();

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties.Where(c => !c.IsAutoIncrement))
			{
				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[propertyDescriptor.PropertyName]);
					
					if (propertyDescriptor.PropertyType == typeof(short))
					{
						generator.Emit(OpCodes.Ldc_I4_0);
					}
					else if (propertyDescriptor.PropertyType == typeof(int))
					{
						generator.Emit(OpCodes.Ldc_I4_0);
					}
					else if (propertyDescriptor.PropertyType == typeof(long))
					{
						generator.Emit(OpCodes.Ldc_I8, 0L);
					}
					else if (propertyDescriptor.PropertyType == typeof(Guid))
					{
						generator.Emit(OpCodes.Ldsfld, FieldInfoFastRef.GuidEmptyGuid);
					}
					else
					{
						var local = generator.DeclareLocal(propertyDescriptor.PropertyType);

						generator.Emit(OpCodes.Ldloca, local);
						generator.Emit(OpCodes.Initobj, local.LocalType);

						generator.Emit(OpCodes.Ldloc, local);
					}

					EmitCompareEquals(generator, propertyDescriptor.PropertyType);
					generator.Emit(OpCodes.Brtrue, returnFalseLabel);
				}
				else
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[propertyDescriptor.PropertyName]);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, returnFalseLabel);

					if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.valueFields[propertyDescriptor.PropertyName]);
						generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.PrimaryKeyIsCommitReady).GetGetMethod());
						generator.Emit(OpCodes.Brfalse, returnFalseLabel);
					}
				}
			}

			generator.MarkLabel(returnTrueLabel);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(returnFalseLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private ColumnInfo[] GetColumnsGeneratedOnTheServerSide()
		{
			return QueryBinder.GetColumnInfos
			(
				 this.typeDescriptorProvider,
				 this.typeDescriptor.PersistedProperties,
				 (c, d) => false,
				 (c, d) => c.IsPropertyThatIsCreatedOnTheServerSide
			);
		}

		private void BuildGetPropertiesGeneratedOnTheServerSideMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var columnInfos = this.GetColumnsGeneratedOnTheServerSide();

			var arrayLocal = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, columnInfos.Length);
			generator.Emit(OpCodes.Newarr, typeof(ObjectPropertyValue));
			generator.Emit(OpCodes.Stloc, arrayLocal);

			var index = 0;

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				var valueField = this.valueFields[columnInfo.DefinitionProperty.PropertyName];

				this.EmitPropertyValue(generator, arrayLocal, valueField.FieldType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					return valueField.FieldType;
				});

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildRelatedDataAccessObjectsProperty(PropertyInfo propertyInfo, TypeBuildContext typeBuildContext)
		{
			PropertyBuilder propertyBuilder;
			FieldBuilder currentFieldInDataObject;

			if (typeBuildContext.IsFirstPass())
			{
				propertyBuilder = this.typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, CallingConventions.HasThis | CallingConventions.Standard, propertyInfo.PropertyType, null, null, null, null, null);
				this.propertyBuilders[propertyBuilder.Name] = propertyBuilder;

				var attributeBuilder = new CustomAttributeBuilder(TypeUtils.GetConstructor(() => new NonSerializedAttribute()), new object[0]);

				currentFieldInDataObject = this.dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Public);
				currentFieldInDataObject.SetCustomAttribute(attributeBuilder);

				this.valueFields[propertyInfo.Name] = currentFieldInDataObject;
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyInfo.Name];
				currentFieldInDataObject = this.valueFields[propertyBuilder.Name];

				propertyBuilder.SetGetMethod(this.BuildRelatedDataAccessObjectsMethod(propertyInfo.Name, propertyInfo.GetGetMethod().Attributes, propertyInfo.GetGetMethod().CallingConvention, propertyInfo.PropertyType, this.typeBuilder, this.dataObjectField, currentFieldInDataObject, propertyInfo));
			}
		}
		
		private MethodBuilder BuildRelatedDataAccessObjectsMethod(string propertyName, MethodAttributes propertyAttributes, CallingConventions callingConventions, Type propertyType, TypeBuilder typeBuilder, FieldInfo dataObjectField, FieldInfo currentFieldInDataObject, PropertyInfo propertyInfo)
		{
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (propertyAttributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = typeBuilder.DefineMethod("get_" + propertyName, methodAttributes, callingConventions, propertyType, Type.EmptyTypes);
			var generator = methodBuilder.GetILGenerator();

			var constructor = currentFieldInDataObject.FieldType.GetConstructor(new [] { typeof(DataAccessModel), typeof(IDataAccessObjectAdvanced), typeof(string) });
    
			var local = generator.DeclareLocal(currentFieldInDataObject.FieldType);
            
			var returnLabel = generator.DefineLabel();

			// Load field and store in temp variable
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
			generator.Emit(OpCodes.Stloc, local);

			// Compare field (temp) to null
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Brtrue, returnLabel);

			// Load "this.DataAccessModel"
			generator.Emit(OpCodes.Ldarg_0);

			//generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("DataAccessModel", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<DataAccessObject>(c => c.GetDataAccessModel()));
			
			// Load "this"
			generator.Emit(OpCodes.Ldarg_0);

			// Load property name
			generator.Emit(OpCodes.Ldstr, propertyName);

			generator.Emit(OpCodes.Newobj, constructor);
			generator.Emit(OpCodes.Stloc, local);

			// Store object
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
			
			// Return local
			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ret);

			return methodBuilder;
		}

		public static PropertyInfo GetPropertyInfo(Type type, string name)
		{
			return type.GetProperties().First(c => c.Name == name);
		}

		private void BuildSetComputedPropertyMethod(PropertyInfo propertyInfo, TypeBuildContext typeBuildContext)
		{
			FieldBuilder fieldBuilder;
			MethodBuilder methodBuilder;
			var attribute = propertyInfo.GetFirstCustomAttribute<ComputedMemberAttribute>(true);

			if (typeBuildContext.IsFirstPass())
			{
				fieldBuilder = this.typeBuilder.DefineField("$$$" + propertyInfo.Name + "ComputeFunc", typeof(Func<,>).MakeGenericType(this.typeBuilder.BaseType, propertyInfo.PropertyType), FieldAttributes.Public);

				const MethodAttributes methodAttributes = MethodAttributes.Public;

				methodBuilder = this.typeBuilder.DefineMethod("$$SetComputedProperty" + propertyInfo.Name, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, typeof(void), null);

				this.setComputedValueMethods[propertyInfo.Name] = methodBuilder;
				this.computedFuncFields[propertyInfo.Name] = fieldBuilder;
			}
			else
			{
				methodBuilder = this.setComputedValueMethods[propertyInfo.Name];
				fieldBuilder = this.computedFuncFields[propertyInfo.Name];
			}

			if (typeBuildContext.IsSecondPass())
			{
				var generator = methodBuilder.GetILGenerator();

				var skip = generator.DefineLabel();

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, fieldBuilder);
				generator.Emit(OpCodes.Brtrue, skip);

				var lambdaLocal = generator.DeclareLocal(typeof(LambdaExpression));
				var computedMemberAttribute = generator.DeclareLocal(typeof(ComputedMemberAttribute));
				var propertyInfoLocal = generator.DeclareLocal(typeof(PropertyInfo));

				generator.Emit(OpCodes.Ldtoken, propertyInfo.DeclaringType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandleMethod);
				generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
				generator.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
				generator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("GetProperty", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(BindingFlags) }, null));
				generator.Emit(OpCodes.Stloc, propertyInfoLocal);

				generator.Emit(OpCodes.Ldloc, propertyInfoLocal);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Call, typeof(MemberInfoUtils).GetMethod("GetFirstCustomAttribute", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(ComputedMemberAttribute)));

				generator.Emit(OpCodes.Ldloc, propertyInfoLocal);
				generator.Emit(OpCodes.Callvirt, computedMemberAttribute.LocalType.GetMethod("GetGetLambdaExpression", BindingFlags.Instance | BindingFlags.Public));
				generator.Emit(OpCodes.Stloc, lambdaLocal);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldloc, lambdaLocal);
				generator.Emit(OpCodes.Callvirt, typeof(LambdaExpression).GetMethod("Compile", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null));
				generator.Emit(OpCodes.Castclass, fieldBuilder.FieldType);
				generator.Emit(OpCodes.Stfld, fieldBuilder);

				generator.MarkLabel(skip);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, fieldBuilder);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Callvirt, fieldBuilder.FieldType.GetMethod("Invoke"));

				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[ForceSetPrefix + propertyInfo.Name].GetSetMethod());

				generator.Emit(OpCodes.Ret);
			}
		}

		private void BuildSetComputedTextPropertyMethod(PropertyInfo propertyInfo, TypeBuildContext typeBuildContext)
		{
			MethodBuilder methodBuilder;
			var attribute = propertyInfo.GetFirstCustomAttribute<ComputedTextMemberAttribute>(true);

			if (attribute == null)
			{
				return;
			}
            
			if (typeBuildContext.IsFirstPass())
			{
				const MethodAttributes methodAttributes = MethodAttributes.Public;

				methodBuilder = this.typeBuilder.DefineMethod("$$SetComputedProperty" + propertyInfo.Name, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, typeof(void), null);

				this.setComputedValueMethods[propertyInfo.Name] = methodBuilder;
			}
			else
			{
				methodBuilder = this.setComputedValueMethods[propertyInfo.Name];
			}

			if (typeBuildContext.IsSecondPass())
			{
				var propertiesToLoad = new List<PropertyInfo>();
				var generator = methodBuilder.GetILGenerator();

				var formatString = VariableSubstituter.Substitute(attribute.Format, this.typeDescriptor);

				formatString = ComputedTextMemberAttribute.FormatRegex.Replace
				(
					formatString, c =>
					{
						PropertyInfo pi;
						PropertyBuilder pb; 
						var name = c.Groups[1].Value;

						if (!this.propertyBuilders.TryGetValue(name, out pb))
						{
							pi = this.baseType.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
						}
						else
						{
							pi = pb;
						}

						propertiesToLoad.Add(pi);

					    return "{" + (propertiesToLoad.Count - 1) + "}";
					}
				);

				foreach (var propertyDescriptor in propertiesToLoad
					.SelectMany(c =>
					{
						var attributes = this.typeDescriptor.Type.GetProperty(c.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetCustomAttributes(typeof(DependsOnPropertyAttribute), true);

						if (attributes.Length > 0)
						{
							return attributes.OfType<DependsOnPropertyAttribute>().Select(d => this.typeDescriptor.GetPropertyDescriptorByPropertyName(d.PropertyName));
						}

						return new[] { this.typeDescriptor.GetPropertyDescriptorByPropertyName(c.Name) };
					})
					.Where(c => c.IsPropertyThatIsCreatedOnTheServerSide))
				{
					var next = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyDescriptor.PropertyName]);
					generator.Emit(OpCodes.Brtrue, next);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(next);
				}

				var arrayLocal = generator.DeclareLocal(typeof(Object[]));

				generator.Emit(OpCodes.Ldc_I4, propertiesToLoad.Count);
				generator.Emit(OpCodes.Newarr, typeof(object));
				generator.Emit(OpCodes.Stloc, arrayLocal);

				var i = 0;

				foreach (var componentPropertyInfo in propertiesToLoad)
				{
					generator.Emit(OpCodes.Ldloc, arrayLocal);
					generator.Emit(OpCodes.Ldc_I4, i);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Callvirt, componentPropertyInfo.GetGetMethod(true));
					
					if (componentPropertyInfo.PropertyType.IsValueType)
					{
						generator.Emit(OpCodes.Box, componentPropertyInfo.PropertyType);
					}

					generator.Emit(OpCodes.Stelem, typeof(object));

					i++;
				}

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldstr, formatString);
				generator.Emit(OpCodes.Ldloc, arrayLocal);
				generator.Emit(OpCodes.Call, typeof(String).GetMethod("Format",  new[]{ typeof(string), typeof(object[]) }));
				generator.Emit(OpCodes.Call, this.propertyBuilders[ForceSetPrefix + propertyInfo.Name].GetSetMethod());
				generator.Emit(OpCodes.Ret);
			}
		}

		private void BuildPersistedProperty(PropertyInfo propertyInfo, TypeBuildContext typeBuildContext)
		{
			PropertyBuilder propertyBuilder;
			FieldBuilder currentFieldInDataObject;
			FieldBuilder valueChangedFieldInDataObject;

			var propertyType = propertyInfo.PropertyType;

			if (typeBuildContext.IsFirstPass())
			{
				currentFieldInDataObject = this.dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyType, FieldAttributes.Public);
				this.valueFields[propertyInfo.Name] = currentFieldInDataObject;

				valueChangedFieldInDataObject = this.dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + HasChangedSuffix, typeof(bool), FieldAttributes.Public);
				this.valueChangedFields[propertyInfo.Name] = valueChangedFieldInDataObject;

				var valueChangedAttributeBuilder = new CustomAttributeBuilder(TypeUtils.GetConstructor(() => new NonSerializedAttribute()), new object[0]);
				valueChangedFieldInDataObject.SetCustomAttribute(valueChangedAttributeBuilder);

				var valueIsSetFieldInDataObject = this.dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + IsSetSuffix, typeof(bool), FieldAttributes.Public);
				var valueIsSetAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				valueIsSetFieldInDataObject.SetCustomAttribute(valueIsSetAttributeBuilder);
				this.valueIsSetFields.Add(propertyInfo.Name, valueIsSetFieldInDataObject);

				propertyBuilder = this.typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyType, null, null, null, null, null);
				
				this.propertyBuilders[propertyInfo.Name] = propertyBuilder;
			}
			else
			{
				currentFieldInDataObject = this.valueFields[propertyInfo.Name];
				valueChangedFieldInDataObject = this.valueChangedFields[propertyInfo.Name];
				propertyBuilder = this.propertyBuilders[propertyInfo.Name];
			}

			this.BuildPropertyMethod(PropertyMethodType.Set, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, typeBuildContext);
			this.BuildPropertyMethod(PropertyMethodType.Get, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, typeBuildContext);
		}

		public static readonly MethodInfo GenericStaticAreEqualMethod = typeof(DataAccessObjectTypeBuilder).GetMethod("AreEqual");
		public static readonly MethodInfo GenericStaticNullableAreEqualMethod = typeof(DataAccessObjectTypeBuilder).GetMethod("NullableAreEqual");

		public static bool AreEqual<T>(T left, T right)
			where T : class
		{
			return Equals(left, right);
		}

		public static bool NullableAreEqual<T>(T? left, T? right)
			where T : struct
		{
			return left.Equals(right);
		}

		private static void EmitCompareEquals(ILGenerator generator, Type operandType)
		{
			if (operandType.IsPrimitive || operandType.IsEnum)
			{	
				generator.Emit(OpCodes.Ceq);
			}
			else if (operandType == typeof(string))
			{
				generator.Emit(OpCodes.Call, MethodInfoFastRef.StringStaticEqualsMethod);
			}
			else
			{
				var equalityOperatorMethod = operandType.GetMethods().FirstOrDefault(c => c.Name == ("op_Equality") && c.GetParameters().Length == 2);

				if (equalityOperatorMethod != null)
				{
					generator.Emit(OpCodes.Call, equalityOperatorMethod);
				}
				else
				{
					if (Nullable.GetUnderlyingType(operandType) != null)
					{
						generator.Emit(OpCodes.Call, GenericStaticNullableAreEqualMethod.MakeGenericMethod(Nullable.GetUnderlyingType(operandType)));
					}
					else
					{
						generator.Emit(OpCodes.Call, GenericStaticAreEqualMethod.MakeGenericMethod(operandType));
					}
				}
			}
		}

		private void BuildPropertyMethod(PropertyMethodType propertyMethodType, string propertyName, PropertyInfo propertyInfo, PropertyBuilder propertyBuilder, FieldInfo currentFieldInDataObject, FieldInfo valueChangedFieldInDataObject, TypeBuildContext typeBuildContext)
		{
			Type returnType;
			Type[] parameters;
			MethodBuilder methodBuilder;
			MethodBuilder forcePropertySetMethod = null;

			propertyName = propertyName ?? propertyInfo.Name;

			var currentPropertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyName);
			var shouldBuildForceMethod = currentPropertyDescriptor.IsComputedTextMember || currentPropertyDescriptor.IsComputedMember;
			
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (propertyInfo.GetGetMethod().Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));

			switch (propertyMethodType)
			{
				case PropertyMethodType.Get:
					returnType = propertyBuilder.PropertyType;
					parameters = Type.EmptyTypes;
					break;
				case PropertyMethodType.Set:
					returnType = typeof(void);
					parameters = new[] { propertyBuilder.PropertyType };
					break;
				default:
					return;
			}

			if (typeBuildContext.IsFirstPass())
			{
				methodBuilder = this.typeBuilder.DefineMethod(propertyMethodType.ToString().ToLower() + "_" + propertyName, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, returnType, parameters);

				switch (propertyMethodType)
				{
					case PropertyMethodType.Get:
						propertyBuilder.SetGetMethod(methodBuilder);
						break;
					case PropertyMethodType.Set:
						propertyBuilder.SetSetMethod(methodBuilder);

						if (shouldBuildForceMethod)
						{
							var forcePropertyBuilder = this.typeBuilder.DefineProperty(ForceSetPrefix + propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, null, null, null, null, null);
							forcePropertySetMethod = this.typeBuilder.DefineMethod("set_" + ForceSetPrefix + propertyInfo.Name, methodAttributes, returnType, parameters);

							forcePropertyBuilder.SetSetMethod(forcePropertySetMethod);
							this.propertyBuilders[ForceSetPrefix + propertyInfo.Name] = forcePropertyBuilder;
						}
						break;
					default:
						return;
				}

				return;
			}
			else
			{
				switch (propertyMethodType)
				{
					case PropertyMethodType.Get:
						methodBuilder = (MethodBuilder)propertyBuilder.GetGetMethod();
						
						break;
					case PropertyMethodType.Set:
						methodBuilder = (MethodBuilder)propertyBuilder.GetSetMethod();
						if (shouldBuildForceMethod)
						{
							forcePropertySetMethod = (MethodBuilder)this.propertyBuilders[ForceSetPrefix + propertyInfo.Name].GetSetMethod();
						}
						break;
					default:
						return;
				}
			}

			var generator = methodBuilder.GetILGenerator();
			var label = generator.DefineLabel();
			
			switch (propertyMethodType)
			{
				case PropertyMethodType.Get:
					if (!currentPropertyDescriptor.IsPrimaryKey)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
						generator.Emit(OpCodes.Brfalse, label);

						if (valueChangedFieldInDataObject != null)
						{
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, this.dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueChangedFieldInDataObject);
							generator.Emit(OpCodes.Brtrue, label);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Call, TypeUtils.GetMethod(() => default(DataAccessObject).Inflate<DataAccessObject>()));
							generator.Emit(OpCodes.Pop);
						}
					}

					generator.MarkLabel(label);

					var propertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyInfo.Name);

					var loadAndReturnLabel = generator.DefineLabel();

					if (propertyDescriptor.IsComputedTextMember || propertyDescriptor.IsComputedMember)
					{
						// if (!this.data.PropertyIsSet)

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyInfo.Name]);
						generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, this.setComputedValueMethods[propertyInfo.Name]);
					}

					// If (PrimaryKey && AutoIncrement)

					if (currentPropertyDescriptor.IsPrimaryKey && currentPropertyDescriptor.IsAutoIncrement)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyName]);
						generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

						// LogicalNot allowed to access primary key property if it's not set (not yet set by DB)

						generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						generator.Emit(OpCodes.Newobj, TypeUtils.GetConstructor(() => new InvalidPrimaryKeyPropertyAccessException(default(string))));
						generator.Emit(OpCodes.Throw);
					}

					generator.MarkLabel(loadAndReturnLabel);

					// Load value and return
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
					generator.Emit(OpCodes.Ret);

					break;
				case PropertyMethodType.Set:
					ILGenerator privateGenerator;
					var notDeletedLabel = generator.DefineLabel();

					// Throw if object has been deleted

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
					generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brfalse, notDeletedLabel);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Newobj, TypeUtils.GetConstructor(() => new DeletedDataAccessObjectException(default(IDataAccessObjectAdvanced))));
					generator.Emit(OpCodes.Throw);

					generator.MarkLabel(notDeletedLabel);

					// Skip setting if value is reference equal

					if (propertyBuilder.PropertyType.IsClass && propertyBuilder.PropertyType != typeof(string))
					{
						var skipLabel = generator.DefineLabel();

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
						generator.Emit(OpCodes.Ldarg_1);
						generator.Emit(OpCodes.Ceq);
						generator.Emit(OpCodes.Brfalse, skipLabel);

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyName]);
						generator.Emit(OpCodes.Brfalse, skipLabel);

						this.EmitUpdateComputedProperties(generator, propertyBuilder.Name, currentPropertyDescriptor.IsPrimaryKey);

						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(skipLabel);
					}

					if (shouldBuildForceMethod)
					{
						privateGenerator = forcePropertySetMethod.GetILGenerator();
						
						generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						generator.Emit(OpCodes.Newobj, TypeUtils.GetConstructor(() => new InvalidPropertyAccessException(default(string))));
						generator.Emit(OpCodes.Throw);
						generator.Emit(OpCodes.Ret);
						
						generator.Emit(OpCodes.Ret);
					}
					else
					{
						privateGenerator = generator;
					}

					// Skip setting if value is the same as the previous value

					var unwrappedNullableType = propertyBuilder.PropertyType.GetUnwrappedNullableType();

					var continueLabel = privateGenerator.DefineLabel();
					
					if ((unwrappedNullableType.IsPrimitive
							|| unwrappedNullableType.IsEnum
							|| unwrappedNullableType == typeof(string)
							|| unwrappedNullableType == typeof(Guid)
							|| unwrappedNullableType == typeof(DateTime)
							|| unwrappedNullableType == typeof(TimeSpan)
							|| unwrappedNullableType == typeof(decimal)))
					{
						// Load old value
						privateGenerator.Emit(OpCodes.Ldarg_1);

						// Load new value
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
						
						// Compare
						EmitCompareEquals(privateGenerator, propertyBuilder.PropertyType);

						privateGenerator.Emit(OpCodes.Brfalse, continueLabel);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyName]);
						privateGenerator.Emit(OpCodes.Brfalse, continueLabel);

						this.EmitUpdateComputedProperties(generator, propertyBuilder.Name, currentPropertyDescriptor.IsPrimaryKey);

						privateGenerator.Emit(OpCodes.Ret);
					}

					privateGenerator.MarkLabel(continueLabel);

					if (currentPropertyDescriptor.IsPrimaryKey)
					{
						var skipSaving = privateGenerator.DefineLabel();

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.IsNew).GetGetMethod());
						privateGenerator.Emit(OpCodes.Brtrue, skipSaving);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
						privateGenerator.Emit(OpCodes.Brtrue, skipSaving);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, this.finishedInitializingField);
						privateGenerator.Emit(OpCodes.Brfalse, skipSaving);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.originalPrimaryKeyField);
						privateGenerator.Emit(OpCodes.Brtrue, skipSaving);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessObjectAdvanced>(c => c.GetPrimaryKeysFlattened()));
						privateGenerator.Emit(OpCodes.Stfld, this.originalPrimaryKeyField);

						privateGenerator.MarkLabel(skipSaving);
					}

					if (valueChangedFieldInDataObject != null)
					{
						// Set value changed field
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldc_I4_1);
						privateGenerator.Emit(OpCodes.Stfld, valueChangedFieldInDataObject);
					}

					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
					privateGenerator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
					privateGenerator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Changed);
					privateGenerator.Emit(OpCodes.Or);
					privateGenerator.Emit(OpCodes.Stfld, this.partialObjectStateField);

					// Set value is set field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
					privateGenerator.Emit(OpCodes.Ldc_I4_1);
					privateGenerator.Emit(OpCodes.Stfld, this.valueIsSetFields[propertyName]);

					// Set the value field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
					privateGenerator.Emit(OpCodes.Ldarg_1);
					privateGenerator.Emit(OpCodes.Stfld, this.valueFields[propertyName]);
	
					var skipCachingObjectLabel = privateGenerator.DefineLabel();

					if (currentPropertyDescriptor.IsPrimaryKey)
					{
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.PrimaryKeyIsCommitReady).GetGetMethod());
						privateGenerator.Emit(OpCodes.Brfalse, skipCachingObjectLabel);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, this.finishedInitializingField);
						privateGenerator.Emit(OpCodes.Brfalse, skipCachingObjectLabel);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.SubmitToCache()));
						privateGenerator.Emit(OpCodes.Pop);
					}

					privateGenerator.MarkLabel(skipCachingObjectLabel);
					this.EmitUpdateComputedProperties(privateGenerator, propertyBuilder.Name, currentPropertyDescriptor.IsPrimaryKey);

					privateGenerator.Emit(OpCodes.Ret);

					break;
			}
		}

		public static void GenerateNullOrDefault(ILGenerator generator, Type type)
		{
			if (type.IsValueType)
			{
				var valueLocal = generator.DeclareLocal(type);

				generator.Emit(OpCodes.Ldloca, valueLocal);
				generator.Emit(OpCodes.Initobj, valueLocal.LocalType);
				generator.Emit(OpCodes.Ldloc, valueLocal);
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}
		}

		private static readonly Type DictionaryType = typeof(Dictionary<string, int>);
		private static readonly ConstructorInfo DictionaryConstructor = typeof(Dictionary<string, int>).GetConstructor(new[] { typeof(int) });
		private static readonly MethodInfo DictionaryAddMethod = typeof(Dictionary<string, int>).GetMethod("Add", new[] { typeof(string), typeof(int) });
		private static readonly MethodInfo DictionaryTryGetValueMethod = typeof(Dictionary<string, int>).GetMethod("TryGetValue", new[] { typeof(string), Type.GetType("System.Int32&")});
		
		/// <summary>
		/// Builds the HasPropertyChanged method.
		/// </summary>
		private void BuildHasPropertyChangedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var jumpTableList = new List<Label>();
			var retLabel = generator.DefineLabel();
			var switchLabel = generator.DefineLabel();
			var indexLocal = generator.DeclareLocal(typeof(int));
			var properties = this.typeDescriptor.PersistedProperties.ToArray();
			var staticDictionaryField = this.typeBuilder.DefineField("$$HasPropertyChanged$$Switch$$", DictionaryType, FieldAttributes.Private | FieldAttributes.Static);

			// if (propertyName == null) return;
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse, retLabel);

			// if ($$HasPropertyChanged$$Switch$$ == null) { populateDictionary }

			generator.Emit(OpCodes.Volatile);
			generator.Emit(OpCodes.Ldsfld, staticDictionaryField);
			generator.Emit(OpCodes.Brtrue, switchLabel);

			// populateDictionary

			generator.Emit(OpCodes.Ldc_I4, properties.Length);
			generator.Emit(OpCodes.Newobj, DictionaryConstructor);
			
			var j = 0;

			foreach (var propertyDescriptor in properties)
			{
				generator.Emit(OpCodes.Dup);
				generator.Emit(OpCodes.Ldstr, propertyDescriptor.PropertyName);
				generator.Emit(OpCodes.Ldc_I4, j++);
				generator.Emit(OpCodes.Callvirt, DictionaryAddMethod);

				jumpTableList.Add(generator.DefineLabel());
			}

			generator.Emit(OpCodes.Volatile);
			generator.Emit(OpCodes.Stsfld, staticDictionaryField);


			var jumpTable = jumpTableList.ToArray();
			
			// $$HasPropertyChanged$$Switch$$ = populatedDictionary

			generator.MarkLabel(switchLabel);

			// if (!$$HasPropertyChanged$$Switch$$.TryGetValue(nameLocal, out index)) return;
			generator.Emit(OpCodes.Volatile);
			generator.Emit(OpCodes.Ldsfld, staticDictionaryField);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldloca, indexLocal);
			generator.Emit(OpCodes.Callvirt, DictionaryTryGetValueMethod);
			var exceptionLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brfalse, exceptionLabel);

			// switch (value) { casees }
			generator.Emit(OpCodes.Ldloc, indexLocal);
			generator.Emit(OpCodes.Switch, jumpTable);
			// default branch
			generator.Emit(OpCodes.Br, retLabel);

			var local = generator.DeclareLocal(typeof(bool));

			for (var i = 0; i < jumpTable.Length; i++)
			{
				var property = properties[i];
				
				generator.MarkLabel(jumpTable[i]);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.valueChangedFields[property.PropertyName]);
				generator.Emit(OpCodes.Ret);
			}

			generator.MarkLabel(exceptionLabel);

			generator.Emit(OpCodes.Ldstr, "Property '");
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldstr, "' not defined on type '" + this.typeDescriptor.Type.Name + "'");
			generator.Emit(OpCodes.Call, MethodInfoFastRef.StringConcatMethod3);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.InvalidOperationExpceptionConstructor);
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(retLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var i = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				generator.Emit(OpCodes.Ldarg_0);

				// Load array value
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelema, typeof(ObjectPropertyValue));
				generator.Emit(OpCodes.Call, PropertyInfoFastRef.ObjectPropertyValueValueProperty.GetGetMethod());

				var propertyName = propertyDescriptor.PropertyName;

				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, propertyDescriptor.PropertyType);
				}
				else
				{
					generator.Emit(OpCodes.Castclass, propertyDescriptor.PropertyType);
				}
				
				// Call set_PrimaryField metho
				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyName].GetSetMethod());
				
				i++;
			}

			generator.Emit(OpCodes.Ret);
		}

		private void BuildNumberOfPrimaryKeysGeneratedOnServerSideProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide));
			generator.Emit(OpCodes.Ret);
		}

		private void BuildNumberOfPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyCount);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildCompositeKeyTypesProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			if (this.typeDescriptor.PrimaryKeyCount == 0)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else if (this.typeDescriptor.PrimaryKeyCount == 1)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else
			{
				var returnLabel = generator.DefineLabel();
				var keyTypeField = this.typeBuilder.DefineField("$$compositeKeyTypes", typeof(Type[]), FieldAttributes.Static | FieldAttributes.Public);

				generator.Emit(OpCodes.Ldsfld, keyTypeField);
				generator.Emit(OpCodes.Brtrue, returnLabel);

				var i = 0;

				generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyProperties.Count);
				generator.Emit(OpCodes.Newarr, typeof(Type));
				generator.Emit(OpCodes.Stsfld, keyTypeField);
				
				foreach (var primaryKeyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
				{
					generator.Emit(OpCodes.Ldsfld, keyTypeField);
					generator.Emit(OpCodes.Ldc_I4, i);
					generator.Emit(OpCodes.Ldtoken, primaryKeyDescriptor.PropertyType);
					generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandleMethod);
					generator.Emit(OpCodes.Stelem, typeof(Type));
					i++;
				}
				
				generator.MarkLabel(returnLabel);
				generator.Emit(OpCodes.Ldsfld, keyTypeField);
			}

			generator.Emit(OpCodes.Ret);
		}

		private void BuildKeyTypeProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			if (this.typeDescriptor.PrimaryKeyCount == 0)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else if (this.typeDescriptor.PrimaryKeyCount == 1)
			{
				generator.Emit(OpCodes.Ldtoken, this.typeDescriptor.PrimaryKeyProperties.First().PropertyType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandleMethod);
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}

			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetHashCodeAccountForServerGeneratedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var retval = generator.DeclareLocal(typeof(int));

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, retval);

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var next = generator.DefineLabel();

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				if (valueField.FieldType.IsValueType)
				{
					var local = generator.DeclareLocal(valueField.FieldType);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldloca, local);
					generator.Emit(OpCodes.Call, valueField.FieldType.GetMethod("GetHashCode"));
				}
				else
				{
					var local = generator.DeclareLocal(valueField.FieldType);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Brfalse, next);
					generator.Emit(OpCodes.Ldloc, local);

					if (valueField.FieldType.IsDataAccessObjectType())
					{
						generator.Emit(OpCodes.Castclass, typeof(IDataAccessObjectInternal));
						generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.GetHashCodeAccountForServerGenerated()));
					}
					else
					{
						generator.Emit(OpCodes.Callvirt, valueField.FieldType.GetMethod("GetHashCode"));
					}
				}

				generator.Emit(OpCodes.Ldloc, retval);
				generator.Emit(OpCodes.Xor);
				generator.Emit(OpCodes.Stloc, retval);

				generator.MarkLabel(next);
			}

			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetHashCodeMethod()
		{
			var methodInfo = this.typeBuilder.BaseType.GetMethod("GetHashCode", Type.EmptyTypes);

			// Don't override GetHashCode method if it is explicitly declared
			if (methodInfo.DeclaringType == this.typeBuilder.BaseType)
			{
				return;
			}

			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = this.typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Select(c => c.ParameterType).ToArray());

			var generator = methodBuilder.GetILGenerator();

			var retval = generator.DeclareLocal(typeof(int));

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, retval);
			
			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var next = generator.DefineLabel();  
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);
				
				if (valueField.FieldType.IsValueType)
				{
					var local = generator.DeclareLocal(valueField.FieldType);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldloca, local);
					generator.Emit(OpCodes.Call, valueField.FieldType.GetMethod("GetHashCode"));
				}
				else
				{
					var local = generator.DeclareLocal(valueField.FieldType);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Brfalse, next);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Callvirt, valueField.FieldType.GetMethod("GetHashCode"));
				}

				generator.Emit(OpCodes.Ldloc, retval);
				generator.Emit(OpCodes.Xor);
				generator.Emit(OpCodes.Stloc, retval);

				generator.MarkLabel(next);
			}
			
			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildMarkServerSidePropertiesAsAppliedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			
			var columnInfos = this.GetColumnsGeneratedOnTheServerSide();

			if (columnInfos.Length > 0)
			{
				foreach (var property in columnInfos.Select(c => c.DefinitionProperty).Distinct())
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stfld, this.valueChangedFields[property.PropertyName]);
				}

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.ServerSidePropertiesHydrated));
				generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
				generator.Emit(OpCodes.Ret);
			}
			else
			{
				generator.Emit(OpCodes.Ret);
			}
		}

		private void BuildEqualsAccountForServerGeneratedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var local = generator.DeclareLocal(this.typeBuilder);
			var returnFalseLabel = generator.DefineLabel();
			var returnTrueLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Isinst, this.typeBuilder);
			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Stloc, local);
			generator.Emit(OpCodes.Brfalse, returnFalseLabel);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brtrue, returnTrueLabel);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, typeof(IDataAccessObjectInternal));
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys).GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnFalseLabel);

			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Castclass, typeof(IDataAccessObjectInternal));
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys).GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnFalseLabel);

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var label = generator.DefineLabel();
				var valueField = this.valueFields[propertyDescriptor.PropertyName];

				// Load our value
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				// Load operand value
				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				EmitCompareEquals(generator, valueField.FieldType);

				generator.Emit(OpCodes.Brfalse, returnFalseLabel);

				generator.MarkLabel(label);
			}

			generator.MarkLabel(returnTrueLabel);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(returnFalseLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildEqualsMethod()
		{
			var methodInfo = this.typeBuilder.BaseType.GetMethod("Equals", new [] { typeof(object) });

			// Don't override Equals method if it is explicitly declared
			if (methodInfo.DeclaringType == this.typeBuilder.BaseType)
			{
				return;
			}

			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = this.typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Select(c => c.ParameterType).ToArray());

			var generator = methodBuilder.GetILGenerator();

			var local = generator.DeclareLocal(this.typeBuilder);
			var returnLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Isinst, this.typeBuilder);
			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Stloc, local);
			generator.Emit(OpCodes.Brfalse, returnLabel);

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var label = generator.DefineLabel();
				var valueField = this.valueFields[propertyDescriptor.PropertyName];
                
				// Load our value
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				// Load operand value
				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				EmitCompareEquals(generator, valueField.FieldType);

				generator.Emit(OpCodes.Brfalse, returnLabel);
				generator.MarkLabel(label);
			}

			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSwapDataMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var returnLabel = generator.DefineLabel();
			var local = generator.DeclareLocal(this.typeBuilder);

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Castclass, this.typeBuilder);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.swappingField);
			generator.Emit(OpCodes.Brtrue, returnLabel);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Beq, returnLabel);

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Castclass, this.typeBuilder);
			generator.Emit(OpCodes.Stloc, local);

			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Stfld, this.swappingField);

			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Brfalse, label);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Changed);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Changed);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brfalse, label);
			
			foreach (var property in this.typeDescriptor.PersistedProperties)
			{
				var innerLabel = generator.DefineLabel();

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.valueChangedFields[property.PropertyName]);
				generator.Emit(OpCodes.Brfalse, innerLabel);
				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[property.PropertyName].GetGetMethod());

				var name = property.PropertyName;

				if (property.IsComputedTextMember || property.IsComputedMember)
				{
					name = ForceSetPrefix + name;
				}

				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[name].GetSetMethod());

				generator.MarkLabel(innerLabel);
			}

			generator.MarkLabel(label);



			foreach (var relatedProperty in this.typeDescriptor.GetRelationshipInfos().Where(c => c.RelationshipType == RelationshipType.ParentOfOneToMany))
			{
				var field = this.valueFields[relatedProperty.ReferencingProperty.PropertyName];

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, field);
				generator.Emit(OpCodes.Stfld, field);
			}

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);

			// this.data = local.data
			generator.Emit(OpCodes.Stfld, this.dataObjectField);

			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stfld, this.swappingField);

			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ret);

		}

		private Type GetBaseType(TypeBuilder builder)
		{
			var type = builder.BaseType;

			while (type.BaseType != typeof(object))
			{
				type = type.BaseType;
			}

			return type;
		}

		private void BuildFinishedInitializingMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Stfld, this.finishedInitializingField);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildResetModifiedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var returnLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.IsDeleted).GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnLabel);

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndBackReferenceProperties)
			{
				var changedFieldInfo = this.valueChangedFields[propertyDescriptor.PropertyName];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldc_I4_0);
				generator.Emit(OpCodes.Stfld, changedFieldInfo);
			}

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Unchanged);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);

			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSubmitToCacheMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, this.baseType.GetMethod("GetDataAccessModel", BindingFlags.Public | BindingFlags.Instance));
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<DataAccessModel>(c => c.GetCurrentDataContext(default(bool))));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<DataAccessObjectDataContext>(c => c.CacheObject(default(DataAccessObject), default(bool))));

			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsNewMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ldc_I4, ~(int)ObjectState.New);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsDeflatedReferenceMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Stfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildCompositePrimaryKeyProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildIsDeflatedReferenceProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsDeletedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)0);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ret);
		}

		private IEnumerable<string> GetPropertyNamesAndDependentPropertyNames(IEnumerable<string> propertyNames)
		{
			return this.GetPropertyNamesAndDependentPropertyNames(propertyNames, new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
		}

		private IEnumerable<string> GetPropertyNamesAndDependentPropertyNames(IEnumerable<string> propertyNames, HashSet<string> visited)
		{
			foreach (var propertyName in propertyNames.Where(c => !visited.Contains(c)))
			{
				visited.Add(propertyName);

				yield return propertyName;

				var propertyInfo = this.baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				if (propertyInfo == null)
				{
					continue;
				}

				var referenced = propertyInfo.GetCustomAttributes(typeof(DependsOnPropertyAttribute), true)
					.Cast<DependsOnPropertyAttribute>()
					.Select(c => c.PropertyName)
					.Where(c => !visited.Contains(c))
					.ToArray();

				foreach (var referencedPropertyName in referenced.Where(c => !visited.Contains(c)))
				{
					visited.Add(referencedPropertyName);

					yield return referencedPropertyName;
				}

				foreach (var result in this.GetPropertyNamesAndDependentPropertyNames(referenced, visited))
				{
					yield return result;
				}
			}
		}

		private void EmitUpdateComputedProperties(ILGenerator generator, string changedPropertyName, bool propertyIsPrimaryKey)
		{
			var propertyNames = new List<string>();

			foreach (var propertyDescriptor in this.typeDescriptor.ComputedTextProperties)
			{
				if (this.GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences())
					.Any(referencedPropertyName => referencedPropertyName == changedPropertyName))
				{
					propertyNames.Add(propertyDescriptor.PropertyName);
				}
			}

			foreach (var propertyDescriptor in this.typeDescriptor.ComputedProperties)
			{
				var expression = propertyDescriptor.ComputedMemberAttribute.GetGetLambdaExpression(propertyDescriptor.PropertyInfo);
				var target = expression.Parameters.First();

				var referencedProperties = ReferencedPropertiesGatherer.Gather(expression, target).Select(c => c.Name).ToArray();
				
				if (this.GetPropertyNamesAndDependentPropertyNames(referencedProperties.Concat(propertyDescriptor.PropertyName))
					.Any(referencedPropertyName => referencedPropertyName == changedPropertyName))
				{
					propertyNames.Add(propertyDescriptor.PropertyName);
				}
			}

			if (propertyNames.Count == 0)
			{
				return;
			}

			var label = generator.DefineLabel();

			if (propertyIsPrimaryKey)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
				generator.Emit(OpCodes.Brtrue, label);
			}

			foreach (var methodInfo in propertyNames.Select(name => this.setComputedValueMethods[name]))
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, methodInfo);
			}

			generator.MarkLabel(label);
		}

		private void BuildComputeServerGeneratedIdDependentComputedTextPropertiesMethod()
		{
			var count = 0;
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			
			foreach (var propertyDescriptor in this.typeDescriptor.ComputedTextProperties)
			{
				var computedTextDependsOnAutoIncrementId = this
					.GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences())
					.Select(propertyName => this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyName))
					.Any(referencedPropertyDescriptor => referencedPropertyDescriptor != null && referencedPropertyDescriptor.IsPropertyThatIsCreatedOnTheServerSide);

				if (computedTextDependsOnAutoIncrementId)
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Callvirt, this.setComputedValueMethods[propertyDescriptor.PropertyName]);

					count++;
				}
			}

			generator.Emit(count > 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void EmitPropertyValue(ILGenerator generator, LocalBuilder result, Type valueType, string propertyName, string persistedName, int index, Func<Type> loadValue)
		{
			var value = generator.DeclareLocal(typeof(object));

			var type = loadValue();

			if (type.IsValueType)
			{
				generator.Emit(OpCodes.Box, type);
			}

			generator.Emit(OpCodes.Stloc, value);

			this.EmitPropertyValue(generator, result, valueType, propertyName, persistedName, index, value);
		}

		private void EmitPropertyValue(ILGenerator generator, LocalBuilder result, Type valueType, string propertyName, string persistedName, int index, LocalBuilder value)
		{
			generator.Emit(OpCodes.Ldloc, result);

			if (result.LocalType.IsArray)
			{
				// Load index
				generator.Emit(OpCodes.Ldc_I4, index);
				generator.Emit(OpCodes.Ldelema, typeof(ObjectPropertyValue));
			}

			// Load type
			generator.Emit(OpCodes.Ldtoken, valueType);
			generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandleMethod);

			// Load property name
			generator.Emit(OpCodes.Ldstr, String.Intern(propertyName));

			// Load persisted name
			generator.Emit(OpCodes.Ldstr, String.Intern(persistedName));

			// Load the property name hashcode
			generator.Emit(OpCodes.Ldc_I4, propertyName.GetHashCode());

			// Load the value
			generator.Emit(OpCodes.Ldloc, value);

			if (value.LocalType.IsValueType)
			{
				generator.Emit(OpCodes.Box, value.LocalType);
			}

			// Construct the ObjectPropertyValue
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueConstructor);

			if (result.LocalType.IsArray)
			{
				generator.Emit(OpCodes.Stobj, typeof(ObjectPropertyValue));
			}
			else
			{
				generator.Emit(OpCodes.Callvirt, MethodInfoFastRef.ObjectPropertyValueListAddMethod);
			}
		}

		private void BuildGetRelatedObjectPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var propertyDescriptors = this.typeDescriptor
				.PersistedAndBackReferenceProperties
				.Where(c => c.PropertyType.IsDataAccessObjectType())
				.ToList();

			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, propertyDescriptors.Count);
			generator.Emit(OpCodes.Newarr, retval.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in propertyDescriptors)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];

				this.EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					return valueField.FieldType;
				});
			}

			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var count = this.typeDescriptor.PrimaryKeyProperties.Count();
			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, retval.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];

				this.EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					return valueField.FieldType;
				});
			}
			
			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetPrimaryKeysFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			var columnInfos = QueryBinder.GetPrimaryKeyColumnInfos(this.typeDescriptorProvider, this.typeDescriptor);

			var count = columnInfos.Length;

			var arrayLocal = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, arrayLocal.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, arrayLocal);

			var index = 0;

			var objectVariable = generator.DeclareLocal(typeof(object));
			
			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				this.EmitPropertyValueRecursive(generator, objectVariable, columnInfo, skipLabel, false);
				this.EmitPropertyValue(generator, arrayLocal, columnInfo.DefinitionProperty.PropertyType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, objectVariable);

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetPrimaryKeysForUpdateFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.originalPrimaryKeyField);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.originalPrimaryKeyField);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(label);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, TypeUtils.GetMethod<IDataAccessObjectAdvanced>(c => c.GetPrimaryKeysFlattened()));

			generator.Emit(OpCodes.Ret);
		}

		private void EmitPropertyValueRecursive(ILGenerator generator, LocalBuilder result, ColumnInfo columnInfo, Label continueLabel, bool checkChanged)
		{
			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Stloc, result);

			generator.Emit(OpCodes.Ldarg_0);

			var first = true;
			var changedSet = false;
			var changed = generator.DeclareLocal(typeof(bool));

			var last = columnInfo.DefinitionProperty;
			var done = generator.DefineLabel();

			foreach (var visited in columnInfo.VisitedProperties.Concat(last))
			{
				var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[visited.PropertyInfo.ReflectedType];
				var localDataObjectField = referencedTypeBuilder.dataObjectField;
				var localValueField = referencedTypeBuilder.valueFields[visited.PropertyName];
				var currentObject = generator.DeclareLocal(referencedTypeBuilder.typeBuilder);
				var valueChangedField = referencedTypeBuilder.valueChangedFields[visited.PropertyName];
					
				generator.Emit(OpCodes.Stloc, currentObject);

				if (currentObject.LocalType.IsDataAccessObjectType() && !first)
				{
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);

					if (checkChanged && changedSet)
					{
						var next = generator.DefineLabel();
						
						generator.Emit(OpCodes.Brfalse, next);

						generator.Emit(OpCodes.Ldloc, changed);
						generator.Emit(OpCodes.Brfalse, continueLabel);

						generator.Emit(OpCodes.Ldnull);
						generator.Emit(OpCodes.Stloc, result);
						generator.Emit(OpCodes.Br, done);

						generator.MarkLabel(next);
					}
					else
					{
						generator.Emit(OpCodes.Brtrue, continueLabel);
					}
				}

				if (ReferenceEquals(visited, last))
				{
					var valueIsSetField = referencedTypeBuilder.valueIsSetFields[visited.PropertyName];
				
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, valueIsSetField);
					generator.Emit(OpCodes.Brfalse, continueLabel);

					if (checkChanged)
					{
						if (!changedSet)
						{
							generator.Emit(OpCodes.Ldloc, currentObject);
							generator.Emit(OpCodes.Ldfld, localDataObjectField);
							generator.Emit(OpCodes.Ldfld, valueChangedField);
							generator.Emit(OpCodes.Stloc, changed);

							changedSet = true;
						}

						generator.Emit(OpCodes.Ldloc, changed);
						generator.Emit(OpCodes.Brfalse, continueLabel);
					}

					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, localValueField);

					if (localValueField.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, localValueField.FieldType);
					}

					generator.Emit(OpCodes.Stloc, result);
				}
				else
				{
					if (!changedSet)
					{
						generator.Emit(OpCodes.Ldloc, currentObject);
						generator.Emit(OpCodes.Ldfld, localDataObjectField);
						generator.Emit(OpCodes.Ldfld, valueChangedField);
						generator.Emit(OpCodes.Stloc, changed);

						changedSet = true;
					}

					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, localValueField);
				}

				first = false;
			}

			generator.MarkLabel(done);
		}

		private void BuildGetAllPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			var count = this.typeDescriptor.PersistedAndBackReferenceProperties.Count;

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, typeof(ObjectPropertyValue));
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndBackReferenceProperties)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];

				this.EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					return valueField.FieldType;
				});
			}

			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetChangedPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			var count = this.typeDescriptor.PersistedProperties.Count + this.typeDescriptor.RelationshipRelatedProperties.Count(c => c.BackReferenceAttribute != null);

			var listLocal = generator.DeclareLocal(typeof(List<ObjectPropertyValue>));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueListConstructor);
			generator.Emit(OpCodes.Stloc, listLocal);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndBackReferenceProperties)
			{
				var label = generator.DefineLabel();
				var label2 = generator.DefineLabel();

				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var valueChangedField = this.valueChangedFields[propertyDescriptor.PropertyName];
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueChangedField);
				generator.Emit(OpCodes.Brtrue, label2);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
				generator.Emit(OpCodes.And);
				generator.Emit(OpCodes.Brfalse, label);

				generator.MarkLabel(label2);

				this.EmitPropertyValue(generator, listLocal, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					return valueField.FieldType;
				});
				
				generator.MarkLabel(label);
			}
			
			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildHasAnyChangedPrimaryKeyServerSidePropertiesProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			var columnInfos = QueryBinder.GetColumnInfos(this.typeDescriptorProvider, this.typeDescriptor.PersistedProperties.Where(c => c.IsPropertyThatIsCreatedOnTheServerSide));
		
			var objectVariable = generator.DeclareLocal(typeof(object));

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				this.EmitPropertyValueRecursive(generator, objectVariable, columnInfo, skipLabel, true);

				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Ret);

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetChangedPropertiesFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod(MethodBase.GetCurrentMethod());
			var properties = this.typeDescriptor.PersistedAndBackReferenceProperties.ToList();

			var columnInfos = QueryBinder.GetColumnInfos(this.typeDescriptorProvider, properties);
			var listLocal = generator.DeclareLocal(typeof(List<ObjectPropertyValue>));

			generator.Emit(OpCodes.Ldc_I4, columnInfos.Length);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueListConstructor);
			generator.Emit(OpCodes.Stloc, listLocal);

			var index = 0;
			var objectVariable = generator.DeclareLocal(typeof(object));

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				this.EmitPropertyValueRecursive(generator, objectVariable, columnInfo, skipLabel, true);
				this.EmitPropertyValue(generator, listLocal, columnInfo.DefinitionProperty.PropertyType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, objectVariable);

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}
		
		private static string GetNameOfPropertyBeingImplemented(MethodBase methodInfo)
		{
			return BuildMethodRegex.Replace(methodInfo.Name, c => c.Groups[1].Value);
		}

		private void BuildIsMissingAnyPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			foreach (var primaryKeyProperty in this.typeDescriptor.PrimaryKeyProperties)
			{
				if (primaryKeyProperty.PropertyType.IsDataAccessObjectType())
				{
					var skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(skip);

					skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty(GetNameOfPropertyBeingImplemented(MethodBase.GetCurrentMethod())).GetGetMethod());
					generator.Emit(OpCodes.Brfalse, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(skip);
				}
				else
				{
					var skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(skip);
				}
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildReferencesNewUncommitedRelatedObjectProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			foreach (var property in this.typeDescriptor.PersistedProperties.Where(c => c.PropertyType.IsDataAccessObjectType()))
			{
				var skip = generator.DefineLabel();

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[property.PropertyName]);
				generator.Emit(OpCodes.Brfalse, skip);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.valueFields[property.PropertyName]);
				generator.Emit(OpCodes.Ldnull);
				generator.Emit(OpCodes.Ceq);
				generator.Emit(OpCodes.Brtrue, skip);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.valueFields[property.PropertyName]);
				generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.ObjectState).GetGetMethod());
				generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.New));
				generator.Emit(OpCodes.And);
				generator.Emit(OpCodes.Brfalse, skip);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Ret);
				generator.MarkLabel(skip);
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			foreach (var primaryKeyProperty in this.typeDescriptor.PrimaryKeyProperties)
			{
				if (primaryKeyProperty.IsPropertyThatIsCreatedOnTheServerSide)
				{
					var skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(skip);
				}
				else if (primaryKeyProperty.PropertyType.IsDataAccessObjectType())
				{
					var skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(skip);

					skip = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, skip);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, this.dataObjectField);
					generator.Emit(OpCodes.Ldfld, this.valueFields[primaryKeyProperty.PropertyName]);
					generator.Emit(OpCodes.Callvirt, TypeUtils.GetProperty<IDataAccessObjectAdvanced>(c => c.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys).GetGetMethod());
					generator.Emit(OpCodes.Brfalse, skip);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);
				
					generator.MarkLabel(skip);
				}
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildNumberOfPropertiesGeneratedOnTheServerSideProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			var count = this.typeDescriptor.PersistedProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide);

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildObjectStateProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase.GetCurrentMethod());

			var notDeletedLabel = generator.DefineLabel();
			var local = generator.DeclareLocal(typeof(ObjectState));

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Stloc, local);

			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brfalse, notDeletedLabel);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(notDeletedLabel);

			var breakLabel1 = generator.DefineLabel();

			// Go through foreign keys properties and change local to include missing foreign
			// key flag if necessary

			foreach (var propertyDescriptor in this.typeDescriptor
				.RelationshipRelatedProperties
				.Where(c => c.IsBackReferenceProperty))
			{
				var innerLabel1 = generator.DefineLabel();
				var innerLabel2 = generator.DefineLabel();
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel2);

				// if (PropertyValue.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys) { retval |= ReferencesNewObjectWithServerSideProperties; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternaIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.ReferencesNewObjectWithServerSideProperties);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);
				generator.Emit(OpCodes.Br, breakLabel1);

				generator.MarkLabel(innerLabel1);

				// If (this.PropertyValue.ObjectState & New)  { retval |= ReferencesNewObject; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectObjectState.GetGetMethod());
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
				generator.Emit(OpCodes.And);
				generator.Emit(OpCodes.Brfalse, innerLabel2);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.ReferencesNewObject);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);

				generator.MarkLabel(innerLabel2);
			}

			generator.MarkLabel(breakLabel1);
			generator.Emit(OpCodes.Nop);

			var breakLabel2 = generator.DefineLabel();

			// Go through persisted properties that are object references
			foreach (var propertyDescriptor in this.typeDescriptor
				.PersistedProperties
				.Where(c => c.PropertyType.IsDataAccessObjectType()))
			{
				var innerLabel1 = generator.DefineLabel();
				var innerLabel2 = generator.DefineLabel();
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel2);

				// if (this.PropertyValue.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys) { retval |= ReferencesNewObjectWithServerSideProperties; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternaIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.IsPrimaryKey ? (int)ObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties : (int)ObjectState.ReferencesNewObjectWithServerSideProperties);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);

				generator.MarkLabel(innerLabel1);

				// If (this.PropertyValue.ObjectState & New)  { retval |= ReferencesNewObject; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectObjectState.GetGetMethod());
				generator.Emit(OpCodes.Ldc_I4, (int) ObjectState.New);
				generator.Emit(OpCodes.And);
				generator.Emit(OpCodes.Brfalse, innerLabel2);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.ReferencesNewObject);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);
				
				generator.MarkLabel(innerLabel2);
			}

			generator.MarkLabel(breakLabel2);

			// Return local
			generator.Emit(OpCodes.Ldloc, local); 
			generator.Emit(OpCodes.Ret);
		}

		private ILGenerator CreateGeneratorForReflectionEmittedPropertyGetter(MethodBase methodInfo)
		{
			var match = Regex.Match(methodInfo.Name, "Build(.*)Property");

			if (match.Success)
			{
				return CreateGeneratorForReflectionEmittedPropertyGetter(match.Groups[1].Value);
			}
			else
			{
				throw new InvalidOperationException($"Method {methodInfo.Name} is named incorrectly");
			}
		}

		private ILGenerator CreateGeneratorForReflectionEmittedPropertyGetter(string propertyName)
		{
			var propertyInfo = this.typeBuilder.BaseType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

			if (propertyInfo != null && propertyInfo.GetGetMethod().IsAbstract)
			{
				var methodInfo = propertyInfo.GetGetMethod();
				var methodAttributes = methodInfo.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot);
				var methodBuilder = this.typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Select(c => c.ParameterType).ToArray());

				return methodBuilder.GetILGenerator();
			}
			else
			{
				propertyInfo = typeof(IDataAccessObjectInternal).GetProperty(propertyName);

				if (propertyInfo == null)
				{
					propertyInfo = typeof(IDataAccessObjectAdvanced).GetProperty(propertyName);
				}

				const MethodAttributes methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Final;

				var methodBuilder = this.typeBuilder.DefineMethod(propertyInfo.DeclaringType.FullName + ".get_" + propertyName, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, propertyInfo.PropertyType, Type.EmptyTypes);
				var propertyBuilder = this.typeBuilder.DefineProperty(propertyInfo.DeclaringType.FullName + "." + propertyName, PropertyAttributes.None, propertyInfo.PropertyType, null, null, null, null, null);

				propertyBuilder.SetGetMethod(methodBuilder);

				this.typeBuilder.DefineMethodOverride(methodBuilder, propertyInfo.GetGetMethod());

				return methodBuilder.GetILGenerator();
			}
		}

		private ILGenerator CreateGeneratorForReflectionEmittedMethod(MethodBase methodInfo)
		{
			var match = Regex.Match(methodInfo.Name, "Build(.*)Method");

			if (match.Success)
			{
				return CreateGeneratorForReflectionEmittedMethod(match.Groups[1].Value);
			}
			else
			{
				throw new InvalidOperationException($"Method {methodInfo.Name} is named incorrectly");
			}
		}

		private ILGenerator CreateGeneratorForReflectionEmittedMethod(string methodName)
		{
			MethodAttributes methodAttributes;
			Type methodReturnType;
			Type[] methodParameterTypes;
			CallingConventions callingConventions;
			MethodInfo interfaceMethodBeingOveridden = null;
			var methodInfo = this.typeBuilder.BaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
			
			if (methodInfo == null)
			{
				methodInfo = typeof(IDataAccessObjectAdvanced).GetMethod(methodName) ?? typeof(IDataAccessObjectInternal).GetMethod(methodName);

				interfaceMethodBeingOveridden = methodInfo;
				callingConventions = methodInfo.CallingConvention;
				methodName = typeof(IDataAccessObjectInternal).FullName + "." + methodName;
				methodAttributes = MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName;
				
				methodReturnType = methodInfo.ReturnType;
				methodParameterTypes = methodInfo.GetParameters().Select(c => c.ParameterType).ToArray();
			}
			else
			{
				methodAttributes = methodInfo.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot | MethodAttributes.Final);
				methodReturnType = methodInfo.ReturnType;
				callingConventions = methodInfo.CallingConvention;
				methodParameterTypes = methodInfo.GetParameters().Select(c => c.ParameterType).ToArray();
			}

			var methodBuilder = this.typeBuilder.DefineMethod(methodName, methodAttributes, callingConventions, methodReturnType, methodParameterTypes);

			if (interfaceMethodBeingOveridden != null)
			{
				this.typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethodBeingOveridden);
			}

			return methodBuilder.GetILGenerator();
		}
	}
}
