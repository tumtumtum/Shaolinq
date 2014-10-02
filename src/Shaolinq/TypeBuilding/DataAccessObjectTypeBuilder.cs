// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Shaolinq.Persistence;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessObjectTypeBuilder
	{
		private static readonly MethodInfo ObjectPropertyValueListAddMethod = typeof(List<ObjectPropertyValue>).GetMethod("Add");
		
		internal static readonly string ForceSetPrefix = "ForceSet";
		internal static readonly string HasChangedSuffix = "Changed";
		internal static readonly string ObjectDataFieldName = "data";
		
		public ModuleBuilder ModuleBuilder { get; private set; }
		public AssemblyBuildContext AssemblyBuildContext { get; private set; }

		private readonly Type baseType;
		private TypeBuilder typeBuilder;
		private FieldInfo dataObjectField;
		private ILGenerator cctorGenerator;
		private FieldInfo isDeflatedReferenceField;
		private FieldBuilder partialObjectStateField;
		private TypeBuilder dataObjectTypeTypeBuilder;
		private ConstructorBuilder constructorBuilder;
		private readonly TypeDescriptor typeDescriptor;
		private ConstructorBuilder dataConstructorBuilder;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		
		private readonly Dictionary<string, FieldBuilder> valueFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueIsSetFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueChangedFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, PropertyBuilder> propertyBuilders = new Dictionary<string, PropertyBuilder>();
		private readonly Dictionary<string, MethodBuilder> setComputedValueMethods = new Dictionary<string, MethodBuilder>();

		public DataAccessObjectTypeBuilder(TypeDescriptorProvider typeDescriptorProvider, AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder, Type baseType)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;
			this.baseType = baseType;
			this.ModuleBuilder = moduleBuilder;
			this.AssemblyBuildContext = assemblyBuildContext;

			assemblyBuildContext.TypeBuilders[baseType] = this;

			this.typeDescriptor = GetTypeDescriptor(baseType);
		}

		private TypeDescriptor GetTypeDescriptor(Type type)
		{
			return this.typeDescriptorProvider.GetTypeDescriptor(type);
		}

		public void BuildFirstPhase(int pass)
		{
			ILGenerator constructorGenerator = null;    
			
			if (pass == 1)
			{
				typeBuilder = this.ModuleBuilder.DefineType(baseType.FullName, TypeAttributes.Class | TypeAttributes.Public, baseType);
				dataObjectTypeTypeBuilder = this.ModuleBuilder.DefineType(baseType.FullName + "Data", TypeAttributes.Class | TypeAttributes.Public, typeof(object));
				typeBuilder.AddInterfaceImplementation(typeof(IDataAccessObject));

				// Static constructor

				var staticConstructor = typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

				cctorGenerator = staticConstructor.GetILGenerator();

				// Define constructor for data object type
				dataConstructorBuilder = dataObjectTypeTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
				constructorGenerator = dataConstructorBuilder.GetILGenerator();
				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));
				constructorGenerator.Emit(OpCodes.Ret);

				var attributeBuilder = new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

				dataObjectTypeTypeBuilder.SetCustomAttribute(attributeBuilder);
				partialObjectStateField = dataObjectTypeTypeBuilder.DefineField("PartialObjectState", typeof(ObjectState), FieldAttributes.Public);

				this.isDeflatedReferenceField = dataObjectTypeTypeBuilder.DefineField("IsDeflatedReference", typeof(bool), FieldAttributes.Public);

				constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);

				dataObjectField = typeBuilder.DefineField("data", dataObjectTypeTypeBuilder, FieldAttributes.Public);
			}
        
			// First thing we need to do is create type that will hold all property values
			var type = baseType;

			// Methods that are "override abstract" need to only be visited once
			var alreadyImplementedProperties = new HashSet<string>();

			while (type != null)
			{
				foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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
						var propertyDescriptor = GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						if (propertyInfo.GetGetMethod() == null)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is missing a get accessor", propertyInfo.Name, typeDescriptor.Type.Name);
						}

						if (propertyInfo.GetSetMethod() == null && !propertyDescriptor.IsComputedTextMember)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is missing a set accessor", propertyInfo.Name, typeDescriptor.Type.Name);
						}

						if ((propertyInfo.GetGetMethod().Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is not declared as virtual or abstract", propertyInfo.Name, typeDescriptor.Type.Name);
						}

						if (propertyInfo.GetSetMethod() != null && (propertyInfo.GetSetMethod().Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is not declared as virtual or abstract", propertyInfo.Name, typeDescriptor.Type.Name);
						}

						BuildPersistedProperty(propertyInfo, pass);

						if (pass == 1)
						{
							if (propertyDescriptor.IsComputedTextMember)
							{
								BuildSetComputedPropertyMethod(propertyInfo, pass);
							}
						}
					}
					else if (persistedMemberAttribute != null && propertyInfo.PropertyType.IsDataAccessObjectType())
					{
						var propertyDescriptor = GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						BuildPersistedProperty(propertyInfo, pass);

						this.BuildForeignKeysValidProperty(propertyDescriptor, pass);
					}
					else if (relatedObjectAttribute != null)
					{
						var propertyDescriptor = GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						BuildPersistedProperty(propertyInfo, pass);

						this.BuildForeignKeysValidProperty(propertyDescriptor, pass);
					}
					else if (relatedObjectsAttribute != null)
					{
						BuildRelatedDataAccessObjectsProperty(propertyInfo, pass);
					}
				}

				type = type.BaseType;
			}

			if (pass == 2)
			{
				type = baseType;

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
							var propertyDescriptor = GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

							if (propertyDescriptor.IsComputedTextMember)
							{
								BuildSetComputedPropertyMethod(propertyInfo, pass);
							}
						}
					}

					type = type.BaseType;
				}
			}

			if (pass == 1)
			{
				constructorGenerator.Emit(OpCodes.Ret);

				// Build constructor that takes a data access model
				var secondConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(DataAccessModel) });

				// Call base constructor in constructor
				var secondCtorGenerator = secondConstructorBuilder.GetILGenerator();

				// Call our own default constructor
				secondCtorGenerator.Emit(OpCodes.Ldarg_0);
				secondCtorGenerator.Emit(OpCodes.Call, constructorBuilder);

				// Set the "DataAccessModel" property
				secondCtorGenerator.Emit(OpCodes.Ldarg_0);
				secondCtorGenerator.Emit(OpCodes.Ldarg_1);
				secondCtorGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetMethod("SetDataAccessModel"));
				secondCtorGenerator.Emit(OpCodes.Ret);

				// Return from static constructor
				cctorGenerator.Emit(OpCodes.Ret);
			}

			if (pass == 2)
			{
				// Call base constructor in constructor
				constructorGenerator = constructorBuilder.GetILGenerator();
				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Newobj, dataConstructorBuilder);
				constructorGenerator.Emit(OpCodes.Stfld, dataObjectField);

				foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Where(c => c.IsAutoIncrement && c.PropertyType.NonNullableType() == typeof(Guid)))
				{
					constructorGenerator.Emit(OpCodes.Ldarg_0);
					constructorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuid);
					constructorGenerator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyDescriptor.PropertyName].GetSetMethod());

					EmitUpdatedComputedPropertes(constructorGenerator, propertyDescriptor.PropertyName, propertyDescriptor.IsPrimaryKey);
				}
				
				constructorGenerator.Emit(OpCodes.Ret);

				dataObjectTypeTypeBuilder.CreateType();

				this.ImplementDataAccessObjectMethods();
			}
		}

		private void ImplementDataAccessObjectMethods()
		{
			this.BuildKeyTypeProperty();
			this.BuildPrimaryKeyIsCommitReadyProperty();
			this.BuildNumberOfPrimaryKeysProperty();
			this.BuildNumberOfPrimaryKeysGeneratedOnServerSideProperty();
			this.BuildCompositeKeyTypesProperty();
			this.BuildGetPrimaryKeysMethod();
			this.BuildGetRelatedObjectPropertiesMethod();
			this.BuildSetPrimaryKeysMethod();
			this.BuildGetPrimaryKeysFlattenedMethod();
			this.BuildResetModifiedMethod();
			this.BuildGetAllPropertiesMethod();
			this.BuildComputeServerGeneratedIdDependentComputedTextPropertiesMethod();
			this.BuildGetChangedPropertiesMethod();
			this.BuildGetChangedPropertiesMethodFlattenedMethod();
			this.BuildObjectStateProperty();
			this.BuildNumberOfDirectPropertiesGeneratedOnTheServerSideProperty();
			this.BuildSwapDataMethod();
			this.BuildHasPropertyChangedMethod();
			this.BuildSetIsNewMethod();
			this.BuildSetIsDeflatedReferenceMethod();
			this.BuildIsDeflatedReferenceProperty();
			this.BuildSetIsDeletedMethod();
			this.BuildGetHashCodeMethod();
			this.BuildEqualsMethod();
			this.BuildIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeysMethod();
			this.BuildSetPropertiesGeneratedOnTheServerSideMethod();
			this.BuildGetPropertiesGeneratedOnTheServerSideMethod();

			typeBuilder.CreateType();
		}

		private void EmitDefaultValue(ILGenerator generator, Type type)
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

		protected virtual void BuildPrimaryKeyIsCommitReadyProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("PrimaryKeyIsCommitReady");

			var returnTrueLabel = generator.DefineLabel();
			var returnFalseLabel = generator.DefineLabel();

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties.Where(c => !c.IsAutoIncrement))
			{
				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueFields[propertyDescriptor.PropertyName]);
					
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
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueFields[propertyDescriptor.PropertyName]);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, returnFalseLabel);

					if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueFields[propertyDescriptor.PropertyName]);
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetProperty("PrimaryKeyIsCommitReady").GetGetMethod());
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

		protected virtual ColumnInfo[] GetColumnsGeneratedOnTheServerSide()
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPropertiesGeneratedOnTheServerSide");

			var columnInfos = GetColumnsGeneratedOnTheServerSide();

			var arrayLocal = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, columnInfos.Length);
			generator.Emit(OpCodes.Newarr, typeof(ObjectPropertyValue));
			generator.Emit(OpCodes.Stloc, arrayLocal);

			var index = 0;

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				var valueField = valueFields[columnInfo.DefinitionProperty.PropertyName];

				EmitPropertyValue(generator, arrayLocal, valueField.FieldType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
				});

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildRelatedDataAccessObjectsProperty(PropertyInfo propertyInfo, int pass)
		{
			PropertyBuilder propertyBuilder;
			FieldBuilder currentFieldInDataObject;

			if (pass == 1)
			{
				propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, CallingConventions.HasThis | CallingConventions.Standard, propertyInfo.PropertyType, null, null, null, null, null);
				this.propertyBuilders[propertyInfo.Name] = propertyBuilder;

				var attributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

				currentFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Public);
				currentFieldInDataObject.SetCustomAttribute(attributeBuilder);

				valueFields[propertyInfo.Name] = currentFieldInDataObject;
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyInfo.Name];
				currentFieldInDataObject = valueFields[propertyInfo.Name];

				propertyBuilder.SetGetMethod(BuildRelatedDataAccessObjectsMethod(propertyInfo.Name, propertyInfo.GetGetMethod().Attributes, propertyInfo.GetGetMethod().CallingConvention, propertyInfo.PropertyType, typeBuilder, dataObjectField, currentFieldInDataObject, currentFieldInDataObject.FieldType.GetConstructor(Type.EmptyTypes), EntityRelationshipType.ParentOfOneToMany, propertyInfo));
			}
		}

		protected virtual MethodBuilder BuildRelatedDataAccessObjectsMethod(string propertyName, MethodAttributes propertyAttributes, CallingConventions callingConventions, Type propertyType, TypeBuilder typeBuilder, FieldInfo dataObjectField, FieldInfo currentFieldInDataObject, ConstructorInfo constructorInfo, EntityRelationshipType relationshipType, PropertyInfo propertyInfo)
		{
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (propertyAttributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));

			var methodBuilder = typeBuilder.DefineMethod("get_" + propertyName, methodAttributes, callingConventions, propertyType, Type.EmptyTypes);

			var generator = methodBuilder.GetILGenerator();
            
			generator.DeclareLocal(currentFieldInDataObject.FieldType);
            
			var returnLabel = generator.DefineLabel();

			// Load field and store in temp variable
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
			generator.Emit(OpCodes.Stloc_0);

			// Compare field (temp) to null
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Brtrue, returnLabel);

			// CreateDatabaseAndSchema RelatedDataAccessObjects
			generator.Emit(OpCodes.Newobj, constructorInfo);
			generator.Emit(OpCodes.Stloc_0);

			// Load RelatedDataAccessObjects
			generator.Emit(OpCodes.Ldloc_0);

			// Load "this"
			generator.Emit(OpCodes.Ldarg_0);

			// Load "this.DataAccessModel"
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("DataAccessModel", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());

			// Load relationship type
			generator.Emit(OpCodes.Ldc_I4, (int)relationshipType);

			// Load Property Name
			generator.Emit(OpCodes.Ldstr, propertyInfo.Name);

			// Call "RelatedDataAccessObjects.Initialize"
			generator.Emit(OpCodes.Callvirt, constructorInfo.DeclaringType.GetMethod("Initialize", new [] {typeof(IDataAccessObject), typeof(DataAccessModel), typeof(EntityRelationshipType), typeof(string)}));

			// Store object
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
			
			// Return local
			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ret);

			return methodBuilder;
		}

		public static PropertyInfo GetPropertyInfo(Type type, string name)
		{
			return type.GetProperties().First(c => c.Name == name);
		}

		private void BuildSetComputedPropertyMethod(PropertyInfo propertyInfo, int pass)
		{
			MethodBuilder methodBuilder;
			var attribute = propertyInfo.GetFirstCustomAttribute<ComputedTextMemberAttribute>(true);

			if (attribute == null)
			{
				return;
			}
            
			if (pass == 1)
			{
				const MethodAttributes methodAttributes = MethodAttributes.Public;

				methodBuilder = typeBuilder.DefineMethod("SetComputedProperty" + propertyInfo.Name, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, typeof(void), null);

				setComputedValueMethods[propertyInfo.Name] = methodBuilder;
			}
			else
			{
				methodBuilder = setComputedValueMethods[propertyInfo.Name];
			}

			if (pass == 2)
			{
				var propertiesToLoad = new List<PropertyInfo>();
				var ilGenerator = methodBuilder.GetILGenerator();

				var formatString = VariableSubstitutor.Substitute(attribute.Format, value =>
				{
					switch (value)
					{
						case "$(PERSISTEDTYPENAME)":
							return typeDescriptor.PersistedName;
						case "$(PERSISTEDTYPENAME_LOWER)":
							return typeDescriptor.PersistedName.ToLower();
						case "$(TYPENAME)":
							return typeDescriptor.Type.Name;
						case "$(TYPENAME_LOWER)":
							return typeDescriptor.Type.Name.ToLower();
						default:
							return value;
					}
				});

				formatString = ComputedTextMemberAttribute.FormatRegex.Replace
				(
					formatString, c =>
					{
						PropertyInfo pi;
						PropertyBuilder pb; 
						var name = c.Groups[1].Value;

						if (!propertyBuilders.TryGetValue(name, out pb))
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

				var arrayLocal = ilGenerator.DeclareLocal(typeof(Object[]));

				ilGenerator.Emit(OpCodes.Ldc_I4, propertiesToLoad.Count);
				ilGenerator.Emit(OpCodes.Newarr, typeof(object));
				ilGenerator.Emit(OpCodes.Stloc, arrayLocal);

				var i = 0;

				foreach (var componentPropertyInfo in propertiesToLoad)
				{
					ilGenerator.Emit(OpCodes.Ldloc, arrayLocal);
					ilGenerator.Emit(OpCodes.Ldc_I4, i);
					ilGenerator.Emit(OpCodes.Ldarg_0);
					ilGenerator.Emit(OpCodes.Callvirt, componentPropertyInfo.GetGetMethod(true));
					if (componentPropertyInfo.PropertyType.IsValueType)
					{
						ilGenerator.Emit(OpCodes.Box, componentPropertyInfo.PropertyType);
					}
					ilGenerator.Emit(OpCodes.Stelem, typeof(object));
					i++;
				}

				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Ldstr, formatString);
				ilGenerator.Emit(OpCodes.Ldloc, arrayLocal);
				ilGenerator.Emit(OpCodes.Call, typeof(String).GetMethod("Format",  new[]{ typeof(string), typeof(object[]) }));
				ilGenerator.Emit(OpCodes.Call, propertyBuilders[propertyInfo.Name].GetSetMethod());
				ilGenerator.Emit(OpCodes.Ret);
			}
		}

		private void BuildPersistedProperty(PropertyInfo propertyInfo, int pass)
		{
			PropertyBuilder propertyBuilder;
			FieldBuilder currentFieldInDataObject;
			FieldBuilder valueChangedFieldInDataObject;

			var propertyType = propertyInfo.PropertyType;

			if (pass == 1)
			{
				currentFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyType, FieldAttributes.Public);
				valueFields[propertyInfo.Name] = currentFieldInDataObject;

				valueChangedFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + DataAccessObjectTypeBuilder.HasChangedSuffix, typeof(bool), FieldAttributes.Public);
				valueChangedFields[propertyInfo.Name] = valueChangedFieldInDataObject;

				var valueChangedAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				valueChangedFieldInDataObject.SetCustomAttribute(valueChangedAttributeBuilder);

				var valueIsSetFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + "IsSet", typeof(bool), FieldAttributes.Public);
				var valueIsSetAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				valueIsSetFieldInDataObject.SetCustomAttribute(valueIsSetAttributeBuilder);
				valueIsSetFields.Add(propertyInfo.Name, valueIsSetFieldInDataObject);

				propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyType, null, null, null, null, null);
				
				this.propertyBuilders[propertyInfo.Name] = propertyBuilder;
			}
			else
			{
				currentFieldInDataObject = valueFields[propertyInfo.Name];
				valueChangedFieldInDataObject = valueChangedFields[propertyInfo.Name];
				propertyBuilder = propertyBuilders[propertyInfo.Name];
			}
            
			BuildPropertyMethod(PropertyMethodType.Set, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, pass);
			BuildPropertyMethod(PropertyMethodType.Get, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, pass);
		}

		public static readonly MethodInfo GenericStaticAreEqualMethod = typeof(DataAccessObjectTypeBuilder).GetMethod("AreEqual");
		public static readonly MethodInfo GenericStaticNullableAreEqualMethod = typeof(DataAccessObjectTypeBuilder).GetMethod("NullableAreEqual");

		public static bool AreEqual<T>(T left, T right)
			where T : class
		{
			return Object.Equals(left, right);
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
				var equalityOperatorMethod = operandType.GetMethods().Filter(c => c.Name == ("op_Equality") && c.GetParameters().Length == 2).FirstOrDefault();

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

		protected virtual void BuildPropertyMethod(PropertyMethodType propertyMethodType, string propertyName, PropertyInfo propertyInfo, PropertyBuilder propertyBuilder, FieldInfo currentFieldInDataObject, FieldInfo valueChangedFieldInDataObject, int pass)
		{
			if (propertyName == null)
			{
				propertyName = propertyInfo.Name;
			}

			Type returnType;
			Type[] parameters;
			MethodBuilder methodBuilder;
			MethodBuilder forcePropertySetMethod = null;

			var currentPropertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyName);
			var shouldBuildForceMethods = currentPropertyDescriptor.IsAutoIncrement || currentPropertyDescriptor.IsPrimaryKey;
			
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
					throw new NotSupportedException(propertyMethodType.ToString());
			}

			if (pass == 1)
			{
				methodBuilder = typeBuilder.DefineMethod(propertyMethodType.ToString().ToLower() + "_" + propertyName, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, returnType, parameters);

				switch (propertyMethodType)
				{
					case PropertyMethodType.Get:
						propertyBuilder.SetGetMethod(methodBuilder);
						break;
					case PropertyMethodType.Set:
						propertyBuilder.SetSetMethod(methodBuilder);

						if (shouldBuildForceMethods)
						{
							var forcePropertyBuilder = typeBuilder.DefineProperty(ForceSetPrefix + propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, null, null, null, null, null);
							forcePropertySetMethod = typeBuilder.DefineMethod("set_" + ForceSetPrefix + propertyInfo.Name, methodAttributes, returnType, parameters);

							forcePropertyBuilder.SetSetMethod(forcePropertySetMethod);
							propertyBuilders[ForceSetPrefix + propertyInfo.Name] = forcePropertyBuilder;
						}
						break;
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
						if (shouldBuildForceMethods)
						{
							forcePropertySetMethod = (MethodBuilder)propertyBuilders[ForceSetPrefix + propertyInfo.Name].GetSetMethod();
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
					if (currentPropertyDescriptor == null || !currentPropertyDescriptor.IsPrimaryKey)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("IsDeflatedReference", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(true));
						generator.Emit(OpCodes.Brfalse, label);

						if (valueChangedFieldInDataObject != null)
						{
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueChangedFieldInDataObject);
							generator.Emit(OpCodes.Brtrue, label);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, this.typeDescriptor.Type.GetMethod("Inflate", BindingFlags.Instance | BindingFlags.Public));
						}
					}

					generator.MarkLabel(label);

					var propertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyInfo.Name);

					var loadAndReturnLabel = generator.DefineLabel();

					if (propertyDescriptor.IsComputedTextMember)
					{
						// if (!this.data.PropertyIsSet)

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyInfo.Name]);
						generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, setComputedValueMethods[propertyInfo.Name]);
					}

					// If (PrimaryKey && AutoIncrement)

					if (currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey && currentPropertyDescriptor.IsAutoIncrement)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyName]);
						generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

						// Not allowed to access primary key property if it's not set (not yet set by DB)

						generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						generator.Emit(OpCodes.Newobj, typeof(InvalidPrimaryKeyPropertyAccessException).GetConstructor(new[] { typeof(string) }));
						generator.Emit(OpCodes.Throw);
					}

					generator.MarkLabel(loadAndReturnLabel);

					// Load value and return
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
					generator.Emit(OpCodes.Ret);

					break;
				case PropertyMethodType.Set:
					if (currentPropertyDescriptor.DeclaringTypeDescriptor.TypeName == "Student")
					{
						Console.WriteLine();
					}

					ILGenerator privateGenerator;
					var continueLabel = generator.DefineLabel();
					var notDeletedLabel = generator.DefineLabel();

					// Throw if object has been deleted

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, partialObjectStateField);
					generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brfalse, notDeletedLabel);
					generator.Emit(OpCodes.Newobj, typeof(DeletedDataAccessObjectException).GetConstructor(new Type[0]));
					generator.Emit(OpCodes.Throw);

					generator.MarkLabel(notDeletedLabel);

					var skipLabel = generator.DefineLabel();

					// Skip setting if value is reference equal

					if (propertyBuilder.PropertyType.IsClass)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
						generator.Emit(OpCodes.Ldarg_1);
						generator.Emit(OpCodes.Ceq);
						generator.Emit(OpCodes.Brfalse, skipLabel);
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldc_I4_1);
						generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);
						generator.Emit(OpCodes.Ret);
					}

					generator.MarkLabel(skipLabel);

					// Skip setting if value is the same as the previous value

					var unwrappedNullableType = propertyBuilder.PropertyType.GetUnwrappedNullableType();

					if ((unwrappedNullableType.IsPrimitive
							|| unwrappedNullableType.IsEnum
							|| unwrappedNullableType == typeof(string)
							|| unwrappedNullableType == typeof(Guid)
							|| unwrappedNullableType == typeof(DateTime)
							|| unwrappedNullableType == typeof(TimeSpan)
							|| unwrappedNullableType == typeof(decimal))
							|| unwrappedNullableType.IsDataAccessObjectType())
					{
						// Load the new  value
						generator.Emit(OpCodes.Ldarg_1);

						// Load the old value
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);

						// Compare and load true or false
						EmitCompareEquals(generator, propertyBuilder.PropertyType);
					
						generator.Emit(OpCodes.Brfalse, continueLabel);
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldc_I4_1);
						generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);

						EmitUpdatedComputedPropertes(generator, propertyBuilder.Name, currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey);

						generator.Emit(OpCodes.Ret);
					}

					generator.MarkLabel(continueLabel);

					if (shouldBuildForceMethods)
					{
						var skip1 = generator.DefineLabel();
						var skip2 = generator.DefineLabel();

						privateGenerator = forcePropertySetMethod.GetILGenerator();
						
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetProperty("IsTransient").GetGetMethod());
						generator.Emit(OpCodes.Brfalse, skip1);
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldarg_1);
						generator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyName].GetSetMethod());
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetMethod("ComputeServerGeneratedIdDependentComputedTextProperties"));
						generator.Emit(OpCodes.Pop);
						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(skip1);

						if (!currentPropertyDescriptor.IsAutoIncrement)
						{
							var skipCachingObjectLabel = generator.DefineLabel();

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyName]);
							generator.Emit(OpCodes.Brtrue, skip2);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyName].GetSetMethod());
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetMethod("ComputeServerGeneratedIdDependentComputedTextProperties"));
							generator.Emit(OpCodes.Pop);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetProperty("PrimaryKeyIsCommitReady").GetGetMethod());
							generator.Emit(OpCodes.Brfalse, skipCachingObjectLabel);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, typeDescriptor.Type.GetProperty("DataAccessModel", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());

							generator.Emit(OpCodes.Ldc_I4_0);
							generator.Emit(OpCodes.Callvirt, typeof(DataAccessModel).GetMethod("GetCurrentDataContext", BindingFlags.Public | BindingFlags.Instance));
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldc_I4_0);
							generator.Emit(OpCodes.Callvirt, typeof(DataAccessObjectDataContext).GetMethod("CacheObject", BindingFlags.Public | BindingFlags.Instance));
							generator.Emit(OpCodes.Pop);

							generator.MarkLabel(skipCachingObjectLabel);

							generator.Emit(OpCodes.Ret);
						}

						generator.MarkLabel(skip2);

						if (currentPropertyDescriptor.IsAutoIncrement && currentPropertyDescriptor.IsPrimaryKey)
						{
							generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
							generator.Emit(OpCodes.Newobj, typeof(InvalidPrimaryKeyPropertyAccessException).GetConstructor(new[] { typeof(string) }));
							generator.Emit(OpCodes.Throw);
							generator.Emit(OpCodes.Ret);
						}
						else
						{
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Stfld, valueFields[propertyName]);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, partialObjectStateField);
							generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Changed);
							generator.Emit(OpCodes.Or);
							generator.Emit(OpCodes.Stfld, partialObjectStateField);

							if (valueChangedFieldInDataObject != null)
							{
								generator.Emit(OpCodes.Ldarg_0);
								generator.Emit(OpCodes.Ldfld, dataObjectField);
								generator.Emit(OpCodes.Ldc_I4_1);
								generator.Emit(OpCodes.Stfld, valueChangedFieldInDataObject);
							}

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldc_I4_1);
							generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);
							generator.Emit(OpCodes.Ret);
						}
					}
					else
					{
						privateGenerator = generator;
					}

					if (valueChangedFieldInDataObject != null)
					{
						// Set value changed field
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
						privateGenerator.Emit(OpCodes.Ldc_I4_1);
						privateGenerator.Emit(OpCodes.Stfld, valueChangedFieldInDataObject);
					}

					// Set value changed field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
					privateGenerator.Emit(OpCodes.Ldfld, partialObjectStateField);
					privateGenerator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Changed);
					privateGenerator.Emit(OpCodes.Or);
					privateGenerator.Emit(OpCodes.Stfld, partialObjectStateField);

					// Set value is set field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
					privateGenerator.Emit(OpCodes.Ldc_I4_1);
					privateGenerator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);

					// Set the value field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
					privateGenerator.Emit(OpCodes.Ldarg_1);
					privateGenerator.Emit(OpCodes.Stfld, valueFields[propertyName]);

					EmitUpdatedComputedPropertes(privateGenerator, propertyBuilder.Name, currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey);

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
		protected virtual void BuildHasPropertyChangedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("HasPropertyChanged");

			var jumpTableList = new List<Label>();
			var retLabel = generator.DefineLabel();
			var switchLabel = generator.DefineLabel();
			var indexLocal = generator.DeclareLocal(typeof(int));
			var properties = typeDescriptor.PersistedProperties.ToArray();
			var staticDictionaryField = typeBuilder.DefineField("$$HasPropertyChanged$$Switch$$", DictionaryType, FieldAttributes.Private | FieldAttributes.Static);

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
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueChangedFields[property.PropertyName]);
				generator.Emit(OpCodes.Ret);
			}

			generator.MarkLabel(exceptionLabel);

			generator.Emit(OpCodes.Ldstr, "Property '");
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldstr, "' not defined on type '" + typeDescriptor.Type.Name + "'");
			generator.Emit(OpCodes.Call, MethodInfoFastRef.StringConcatMethod3);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.InvalidOperationExpceptionConstructor);
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(retLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildSetPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetPrimaryKeys");

			var i = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				generator.Emit(OpCodes.Ldarg_0);

				// Load array value
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelema, typeof(ObjectPropertyValue));
				generator.Emit(OpCodes.Call, PropertyInfoFastRef.ObjectPropertyValueValueProperty.GetGetMethod());

				var propertyName = ForceSetPrefix + propertyDescriptor.PropertyName;

				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, propertyDescriptor.PropertyType);
				}
				else
				{
					generator.Emit(OpCodes.Castclass, propertyDescriptor.PropertyType);
				}
				
				// Call set_PrimaryField metho
				generator.Emit(OpCodes.Callvirt, propertyBuilders[propertyName].GetSetMethod());
				
				i++;
			}

			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildNumberOfPrimaryKeysGeneratedOnServerSideProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfPrimaryKeysGeneratedOnServerSide");

			generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide));
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildNumberOfPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfPrimaryKeys");

			generator.Emit(OpCodes.Ldc_I4, typeDescriptor.PrimaryKeyCount);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildCompositeKeyTypesProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("CompositeKeyTypes");

			if (typeDescriptor.PrimaryKeyCount == 0)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else if (typeDescriptor.PrimaryKeyCount == 1)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else
			{
				var returnLabel = generator.DefineLabel();
				var keyTypeField = typeBuilder.DefineField("$$keytype", typeof(Type[]), FieldAttributes.Static | FieldAttributes.Public);

				generator.Emit(OpCodes.Ldsfld, keyTypeField);
				generator.Emit(OpCodes.Brtrue, returnLabel);

				var i = 0;

				generator.Emit(OpCodes.Ldc_I4, typeDescriptor.PrimaryKeyProperties.Count);
				generator.Emit(OpCodes.Newarr, typeof(Type));
				generator.Emit(OpCodes.Stsfld, keyTypeField);
				
				foreach (var primaryKeyDescriptor in typeDescriptor.PrimaryKeyProperties.Sorted((x, y) => x == y ? 0 : x.PropertyName == "Id" ? -1 : 1))
				{
					generator.Emit(OpCodes.Ldsfld, keyTypeField);
					generator.Emit(OpCodes.Ldc_I4, i);
					generator.Emit(OpCodes.Ldtoken, primaryKeyDescriptor.PropertyType);
					generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
					generator.Emit(OpCodes.Stelem, typeof(Type));
					i++;
				}
				
				generator.MarkLabel(returnLabel);
				generator.Emit(OpCodes.Ldsfld, keyTypeField);
			}

			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildKeyTypeProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("KeyType");

			if (typeDescriptor.PrimaryKeyCount == 0)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else if (typeDescriptor.PrimaryKeyCount == 1)
			{
				generator.Emit(OpCodes.Ldtoken, typeDescriptor.PrimaryKeyProperties.First().PropertyType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}

			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetHashCodeMethod()
		{
			var methodInfo = typeBuilder.BaseType.GetMethod("GetHashCode", Type.EmptyTypes);

			// Don't override GetHashCode method if it is explicitly declared
			if (methodInfo.DeclaringType == typeBuilder.BaseType)
			{
				return;
			}

			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Convert(c => c.ParameterType).ToArray());

			var generator = methodBuilder.GetILGenerator();

			var retval = generator.DeclareLocal(typeof(int));

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, retval);
			
			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var valueField = valueFields[propertyDescriptor.PropertyName];
				var next = generator.DefineLabel();  
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
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

		protected virtual void BuildSetPropertiesGeneratedOnTheServerSideMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetPropertiesGeneratedOnTheServerSide");
			var index = 0;

			var columnInfos = GetColumnsGeneratedOnTheServerSide();

			foreach (var columnInfo in columnInfos)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);

				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, index);
				generator.Emit(OpCodes.Ldelem, typeof(object));

				generator.Emit(OpCodes.Ldtoken, columnInfo.DefinitionProperty.PropertyType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.ConvertChangeTypeMethod);

				if (columnInfo.DefinitionProperty.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, columnInfo.DefinitionProperty.PropertyType);
				}
				else
				{
					generator.Emit(OpCodes.Castclass, columnInfo.DefinitionProperty.PropertyType);
				}

				generator.Emit(OpCodes.Stfld, this.valueFields[columnInfo.DefinitionProperty.PropertyName]);
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Stfld, this.valueIsSetFields[columnInfo.DefinitionProperty.PropertyName]);

				index++;
			}

			if (index == 0)
			{
				generator.Emit(OpCodes.Ldstr, "No autoincrement property defined on type: " + typeBuilder.Name);
				generator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
				generator.Emit(OpCodes.Throw);
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.ServerSidePropertiesHydrated);
				generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
				generator.Emit(OpCodes.Ret);
			}
		}

		protected virtual void BuildEqualsMethod()
		{
			var methodInfo = typeBuilder.BaseType.GetMethod("Equals", new [] { typeof(object) });

			// Don't override Equals method if it is explicitly declared
			if (methodInfo.DeclaringType == typeBuilder.BaseType)
			{
				return;
			}

			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (methodInfo.Attributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Convert(c => c.ParameterType).ToArray());

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
				var valueField = valueFields[propertyDescriptor.PropertyName];
                
				// Load our value
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				// Load operand value
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueField);

				EmitCompareEquals(generator, valueField.FieldType);

				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Brfalse, returnLabel);
				}
				else
				{
					generator.Emit(OpCodes.Brtrue, label);

					// False if one of the values is null
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, returnLabel);
					generator.Emit(OpCodes.Ldloc_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, returnLabel);

					// Use Object.Equals(object) method

					// Load our value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					// Load operand value
					generator.Emit(OpCodes.Ldloc_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					generator.Emit(OpCodes.Callvirt, MethodInfoFastRef.ObjectEqualsMethod);

					generator.Emit(OpCodes.Brfalse, returnLabel);
				}

				generator.MarkLabel(label);
			}

			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(returnLabel);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildSwapDataMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SwapData");

			var returnLabel = generator.DefineLabel();
			var local = generator.DeclareLocal(this.typeBuilder);

			
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Beq, returnLabel);

			var label = generator.DefineLabel();


			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Castclass, typeBuilder);
			generator.Emit(OpCodes.Stloc, local);


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


			foreach (var property in typeDescriptor.PersistedProperties)
			{
				var innerLabel = generator.DefineLabel();

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueChangedFields[property.PropertyName]);
				generator.Emit(OpCodes.Brfalse, innerLabel);
				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[property.PropertyName].GetGetMethod());

				var name = property.PropertyName;

				if (property.IsAutoIncrement)
				{
					name = ForceSetPrefix + name;
				}

				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[name].GetSetMethod());

				generator.MarkLabel(innerLabel);
			}

			generator.MarkLabel(label);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);

			// this.data = local
			generator.Emit(OpCodes.Stfld, dataObjectField);

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
		/// <summary>
		/// Builds the "ResetModified" method.
		/// </summary>
		protected virtual void BuildResetModifiedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ResetModified");

			var returnLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetProperty("IsDeleted").GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnLabel);

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndRelatedObjectProperties)
			{
				var changedFieldInfo = valueChangedFields[propertyDescriptor.PropertyName];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldc_I4_0);
				generator.Emit(OpCodes.Stfld, changedFieldInfo);
			}

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Unchanged);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);

			generator.MarkLabel(returnLabel);

			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildSetIsNewMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsNew");

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
			generator.Emit(OpCodes.Stfld, partialObjectStateField);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)0);
			generator.Emit(OpCodes.Stfld, partialObjectStateField);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildSetIsDeflatedReferenceMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsDeflatedReference");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Stfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildIsDeflatedReferenceProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("IsDeflatedReference");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildSetIsDeletedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsDeleted");

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.Stfld, partialObjectStateField);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldc_I4, (int)0);
			generator.Emit(OpCodes.Stfld, partialObjectStateField);
			generator.Emit(OpCodes.Ret);
		}

		private IEnumerable<string> GetPropertyNamesAndDependentPropertyNames(IEnumerable<string> propertyNames)
		{
			foreach (var propertyName in propertyNames)
			{
				yield return propertyName;

				var propertyInfo = this.baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				if (propertyInfo == null)
				{
					continue;
				}

				foreach (DependsOnPropertyAttribute attribute in propertyInfo.GetCustomAttributes(typeof(DependsOnPropertyAttribute), true))
				{
					yield return attribute.PropertyName;
				}
			}
		}

		private void EmitUpdatedComputedPropertes(ILGenerator generator, string changedPropertyName, bool propertyIsPrimaryKey)
		{
			var propertyNames = new List<string>();

			foreach (var propertyDescriptor in typeDescriptor.ComputedTextProperties)
			{
				foreach (var referencedPropertyName in GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences()))
				{
					if (referencedPropertyName == changedPropertyName)
					{
						propertyNames.Add(propertyDescriptor.PropertyName);

						break;
					}
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
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, isDeflatedReferenceField);
				generator.Emit(OpCodes.Brtrue, label);
			}

			foreach (var name in propertyNames)
			{
				var methodInfo = setComputedValueMethods[name];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, methodInfo);
			}

			generator.MarkLabel(label);
		}

		protected virtual void BuildComputeServerGeneratedIdDependentComputedTextPropertiesMethod()
		{
			var count = 0;
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ComputeServerGeneratedIdDependentComputedTextProperties");
			
			foreach (var propertyDescriptor in typeDescriptor.ComputedTextProperties)
			{
				var computedTextDependsOnAutoIncrementId = false;

				foreach (var propertyName in GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences()))
				{
					var referencedPropertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyName);

					if (referencedPropertyDescriptor != null && referencedPropertyDescriptor.IsPropertyThatIsCreatedOnTheServerSide)
					{
						computedTextDependsOnAutoIncrementId = true;

						break;
					}
				}
			
				if (computedTextDependsOnAutoIncrementId)
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Callvirt, setComputedValueMethods[propertyDescriptor.PropertyName]);

					count++;
				}
			}

			generator.Emit(count > 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void EmitPropertyValue(ILGenerator generator, LocalBuilder result, Type valueType, string propertyName, string persistedName, int index, Action loadValue)
		{
			var value = generator.DeclareLocal(typeof(object));

			// Load value
			loadValue();

			if (valueType.IsValueType)
			{
				generator.Emit(OpCodes.Box, valueType);
			}

			generator.Emit(OpCodes.Stloc, value);

			// Load retval
			generator.Emit(OpCodes.Ldloc, result);

			if (result.LocalType.IsArray)
			{
				// Load index
				generator.Emit(OpCodes.Ldc_I4, index);
				generator.Emit(OpCodes.Ldelema, typeof(ObjectPropertyValue));
			}

			// Load type
			generator.Emit(OpCodes.Ldtoken, valueType);
			generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);

			// Load property name
			generator.Emit(OpCodes.Ldstr, String.Intern(propertyName));

			// Load persisted name
			generator.Emit(OpCodes.Ldstr, String.Intern(persistedName));

			// Load the property name hashcode
			generator.Emit(OpCodes.Ldc_I4, propertyName.GetHashCode());

			// Load the value
			generator.Emit(OpCodes.Ldloc, value);

			// Construct the ObjectPropertyValue
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueConstructor);

			if (result.LocalType.IsArray)
			{
				generator.Emit(OpCodes.Stobj, typeof(ObjectPropertyValue));
			}
			else
			{
				generator.Emit(OpCodes.Callvirt, ObjectPropertyValueListAddMethod);
			}
		}

		protected virtual void BuildGetRelatedObjectPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetRelatedObjectProperties");

			var propertyDescriptors = typeDescriptor
				.PersistedAndRelatedObjectProperties
				.Where(c => c.PropertyType.IsDataAccessObjectType())
				.ToList();

			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, propertyDescriptors.Count);
			generator.Emit(OpCodes.Newarr, retval.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in propertyDescriptors)
			{
				var valueField = valueFields[propertyDescriptor.PropertyName];

				EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
				});
			}

			// Return array
			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeys");

			var count = typeDescriptor.PrimaryKeyProperties.Count();
			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, retval.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in typeDescriptor.PrimaryKeyProperties)
			{
				var valueField = valueFields[propertyDescriptor.PropertyName];

				EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
				});
			}

			// Return array
			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetPrimaryKeysFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeysFlattened");
			var columnInfos = QueryBinder.GetPrimaryKeyColumnInfos(this.typeDescriptorProvider, this.typeDescriptor);

			var count = columnInfos.Length;

			var arrayLocal = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, arrayLocal.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, arrayLocal);

			var index = 0;

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				EmitPropertyValue(generator, arrayLocal, columnInfo.DefinitionProperty.PropertyType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, 
					() => this.EmitGetValueRecursive(columnInfo, generator, skipLabel, false, true));

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void EmitGetValueRecursive(ColumnInfo columnInfo, ILGenerator generator, Label skipLabel, bool checkChanged, bool defaultIfNotAvailable)
		{
			var first = true;
			
			generator.Emit(OpCodes.Ldarg_0);

			var last = new Tuple<PropertyDescriptor, string>(columnInfo.DefinitionProperty, columnInfo.DefinitionProperty.PropertyName);

			var isNew = generator.DeclareLocal(typeof(bool));
			
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, isNew);
					
			foreach (var visited in columnInfo
				.VisitedProperties
				.Select(c => new Tuple<PropertyDescriptor, string>(c, c.PropertyName))
				.Append(last))
			{
				var loadValueLabel = generator.DefineLabel();
				var readValueLabel = generator.DefineLabel();
				var gotValueLabel = generator.DefineLabel();
				var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[visited.Item1.PropertyInfo.ReflectedType];
				var valueChangedField = referencedTypeBuilder.valueChangedFields[visited.Item2];
				var valueIsSetField = referencedTypeBuilder.valueIsSetFields[visited.Item2];
				var localDataObjectField = referencedTypeBuilder.dataObjectField;
				var localValueField = referencedTypeBuilder.valueFields[visited.Item2];
				var currentObject = generator.DeclareLocal(referencedTypeBuilder.typeBuilder);
				var value = generator.DeclareLocal(localValueField.FieldType);

				generator.Emit(OpCodes.Stloc, currentObject);

				if (currentObject.LocalType.IsClass && !first)
				{
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brfalse, readValueLabel);

					if (defaultIfNotAvailable)
					{
						EmitDefaultValue(generator, localValueField.FieldType);

						generator.Emit(OpCodes.Stloc, value);
						generator.Emit(OpCodes.Br, gotValueLabel);
					}
					else
					{
						generator.Emit(OpCodes.Br, skipLabel);
					}
				}

				if (!columnInfo.DefinitionProperty.IsPropertyThatIsCreatedOnTheServerSide)
				{
					var l1 = generator.DefineLabel();

					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectObjectState.GetGetMethod());
					generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.ServerSidePropertiesHydrated | ObjectState.New));
					generator.Emit(OpCodes.And);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, l1);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Stloc, isNew);
					generator.MarkLabel(l1);
				}

				first = false;

				generator.MarkLabel(readValueLabel);

				if (checkChanged)
				{
					var l5 = generator.DefineLabel();

					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, valueChangedField);
					generator.Emit(OpCodes.Brfalse, l5);

					/*if (visited.Item1.IsPropertyThatIsCreatedOnTheServerSide && !visited.Item1.IsPrimaryKey)
					{
						generator.Emit(OpCodes.Ldloc, currentObject);
						generator.Emit(OpCodes.Ldfld, localDataObjectField);
						generator.Emit(OpCodes.Ldfld, valueIsSetField);
						generator.Emit(OpCodes.Brfalse, l5);
					}*/

					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Stloc, isNew);
					generator.Emit(OpCodes.Br, loadValueLabel);

					generator.MarkLabel(l5);
				}
				else
				{
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, valueIsSetField);
					generator.Emit(OpCodes.Brtrue, loadValueLabel);
				}

				var l2 = generator.DefineLabel();

				/*if (!first && !columnInfo.DefinitionProperty.IsPropertyThatIsCreatedOnTheServerSide)
				{
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectObjectState.GetGetMethod());
					generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.ServerSidePropertiesHydrated | ObjectState.New));
					generator.Emit(OpCodes.And);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, l2);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Stloc, isNew);
					generator.MarkLabel(l2);
					
				}*/

				generator.Emit(OpCodes.Ldloc, isNew);
				generator.Emit(OpCodes.Brtrue, loadValueLabel);

				if (localValueField.FieldType.IsDataAccessObjectType())
				{
					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, localValueField);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brtrue, loadValueLabel);

					generator.Emit(OpCodes.Ldloc, currentObject);
					generator.Emit(OpCodes.Ldfld, localDataObjectField);
					generator.Emit(OpCodes.Ldfld, localValueField);
					generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectObjectState.GetGetMethod());
					generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.ServerSidePropertiesHydrated | ObjectState.New));
					generator.Emit(OpCodes.And);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ceq);
					generator.Emit(OpCodes.Brfalse, loadValueLabel);
				}

				if (defaultIfNotAvailable)
				{
					EmitDefaultValue(generator, localValueField.FieldType);
					generator.Emit(OpCodes.Stloc, value);
					generator.Emit(OpCodes.Br, gotValueLabel);
				}
				else
				{
					generator.Emit(OpCodes.Br, skipLabel);
				}

				generator.MarkLabel(loadValueLabel);
				generator.Emit(OpCodes.Ldloc, currentObject);
				generator.Emit(OpCodes.Ldfld, localDataObjectField);
				generator.Emit(OpCodes.Ldfld, localValueField);
				generator.Emit(OpCodes.Stloc, value);
				
				generator.MarkLabel(gotValueLabel);
				generator.Emit(OpCodes.Ldloc, value);
			}
		}

		protected virtual void BuildGetAllPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetAllProperties");
			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			var count = this.typeDescriptor.PersistedProperties.Count;

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, typeof(ObjectPropertyValue));
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndRelatedObjectProperties)
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];

				EmitPropertyValue(generator, retval, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
				});
			}

			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetChangedPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetChangedProperties");
			var count = this.typeDescriptor.PersistedProperties.Count + this.typeDescriptor.RelatedProperties.Count(c => c.BackReferenceAttribute != null);

			var listLocal = generator.DeclareLocal(typeof(List<ObjectPropertyValue>));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueListConstructor);
			generator.Emit(OpCodes.Stloc, listLocal);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Concat(this.typeDescriptor.RelatedProperties.Filter(c => c.BackReferenceAttribute != null)))
			{
				if (propertyDescriptor.IsPropertyThatIsCreatedOnTheServerSide)
				{ 
					continue;
				}

				var label = generator.DefineLabel();
				var label2 = generator.DefineLabel();

				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var valueChangedField = this.valueChangedFields[propertyDescriptor.PropertyName];
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueChangedField);
				generator.Emit(OpCodes.Brtrue, label2);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, partialObjectStateField);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
				generator.Emit(OpCodes.Ceq);
				
				generator.Emit(OpCodes.Brfalse, label);

				generator.MarkLabel(label2);

				EmitPropertyValue(generator, listLocal, propertyDescriptor.PropertyType, propertyDescriptor.PropertyName, propertyDescriptor.PersistedName, index++, () =>
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
				});
				
				generator.MarkLabel(label);
			}
			
			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetChangedPropertiesMethodFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetChangedPropertiesFlattened");
			var properties = this.typeDescriptor
				.PersistedProperties
				.Concat(this.typeDescriptor.RelatedProperties.Where(c => c.IsBackReferenceProperty))
				.Where(c => !(c.IsPropertyThatIsCreatedOnTheServerSide && c.IsPrimaryKey))
				.ToList();

			var columnInfos = QueryBinder.GetColumnInfos(this.typeDescriptorProvider, properties);

			var listLocal = generator.DeclareLocal(typeof(List<ObjectPropertyValue>));

			generator.Emit(OpCodes.Ldc_I4, columnInfos.Length);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueListConstructor);
			generator.Emit(OpCodes.Stloc, listLocal);

			var index = 0;

			foreach (var columnInfoValue in columnInfos)
			{
				var columnInfo = columnInfoValue;
				var skipLabel = generator.DefineLabel();

				EmitPropertyValue(generator, listLocal, columnInfo.DefinitionProperty.PropertyType, columnInfo.GetFullPropertyName(), columnInfo.ColumnName, index++, 
					() => this.EmitGetValueRecursive(columnInfo, generator, skipLabel, true, false));

				generator.MarkLabel(skipLabel);
			}

			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}


		private void BuildForeignKeysValidProperty(PropertyDescriptor propertyDescriptor, int pass)
		{
			PropertyBuilder propertyBuilder;
			var propertyName = propertyDescriptor.PropertyName + "ForeignKeysValid";
			var propertyType = typeof(bool);

			if (pass == 1)
			{
				propertyBuilder = typeBuilder.DefineProperty(propertyName, propertyDescriptor.PropertyInfo.Attributes, propertyType, null, null, null, null, null);

				this.propertyBuilders[propertyName] = propertyBuilder;

				const MethodAttributes methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;

				var methodBuilder = typeBuilder.DefineMethod("get_" + propertyName, methodAttributes, CallingConventions.Standard, propertyType, Type.EmptyTypes);

				propertyBuilder.SetGetMethod(methodBuilder);

				propertyBuilders[propertyName] = propertyBuilder;
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyName];
				var methodBuilder = (MethodBuilder)propertyBuilder.GetGetMethod();

				var generator = methodBuilder.GetILGenerator();
				
				foreach (var columnInfoValue in QueryBinder.GetColumnInfos(this.typeDescriptorProvider, propertyDescriptor))
				{
					var nextLabel = generator.DefineLabel();
				
					EmitGetValueRecursive(columnInfoValue, generator, nextLabel, false, true);

					EmitDefaultValue(generator, columnInfoValue.DefinitionProperty.PropertyType);
					EmitCompareEquals(generator, columnInfoValue.DefinitionProperty.PropertyType);

					generator.Emit(OpCodes.Brfalse, nextLabel);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(nextLabel);
				}

				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Ret);
			}
		}

		protected virtual void BuildIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys");

			var columnInfos = QueryBinder.GetColumnInfos
			(
				this.typeDescriptorProvider,
				this.typeDescriptor.PrimaryKeyProperties,
				(c, d) => d == 0 || c.IsPrimaryKey,
				(c, d) => c.IsPropertyThatIsCreatedOnTheServerSide
			);
		
			foreach (var columnInfo in columnInfos)
			{
				this.EmitGetValueRecursive(columnInfo, generator, new Label(), false, true);

				switch (Type.GetTypeCode(columnInfo.DefinitionProperty.PropertyType))
				{

					case TypeCode.Byte:
					case TypeCode.Int16:
						generator.Emit(OpCodes.Ldc_I4_S, 0);
						generator.Emit(OpCodes.Ceq);
						break;
					case TypeCode.Int32:
						generator.Emit(OpCodes.Ldc_I4_0);
						generator.Emit(OpCodes.Ceq);
						break;
					case TypeCode.Int64:
						generator.Emit(OpCodes.Ldc_I8, 0L);
						generator.Emit(OpCodes.Ceq);
						break;
					default:
						throw new NotSupportedException(columnInfo.DefinitionProperty.PropertyType.ToString());
				}

				var label = generator.DefineLabel();

				generator.Emit(OpCodes.Brfalse, label);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Ret);

				generator.MarkLabel(label);
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildNumberOfDirectPropertiesGeneratedOnTheServerSideProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfDirectPropertiesGeneratedOnTheServerSide");

			var count = this.typeDescriptor.PersistedProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide);

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildObjectStateProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("ObjectState");

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
				.RelatedProperties
				.Where(c => c.IsBackReferenceProperty))
			{
				var innerLabel1 = generator.DefineLabel();
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (PropertyValue.IsNew) { retval |= MissingConstrainedForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternaIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.MissingConstrainedForeignKeys);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);
				generator.Emit(OpCodes.Br, breakLabel1);

				generator.MarkLabel(innerLabel1);
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
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (this.PropertyValue.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys) { retval |= MissingUnconstrainedForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternaIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.IsPrimaryKey ? (int)ObjectState.MissingServerGeneratedForeignPrimaryKeys : (int)ObjectState.MissingUnconstrainedForeignKeys);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);
				generator.Emit(OpCodes.Br, breakLabel2);

				generator.MarkLabel(innerLabel1);
			}

			generator.MarkLabel(breakLabel2);

			// Return local
			generator.Emit(OpCodes.Ldloc, local); 
			generator.Emit(OpCodes.Ret);
		}

		private ILGenerator CreateGeneratorForReflectionEmittedPropertyGetter(string propertyName)
		{
			const MethodAttributes methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Final;

			var propertyInfo = typeof(IDataAccessObject).GetProperty(propertyName);

			var methodBuilder = typeBuilder.DefineMethod(typeof(IDataAccessObject).FullName + ".get_" + propertyName, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, propertyInfo.PropertyType, Type.EmptyTypes);
			var propertyBuilder = typeBuilder.DefineProperty(typeof(IDataAccessObject).FullName + "." + propertyName, PropertyAttributes.None, propertyInfo.PropertyType, null, null, null, null, null);

			propertyBuilder.SetGetMethod(methodBuilder);

			typeBuilder.DefineMethodOverride(methodBuilder, typeof(IDataAccessObject).GetMethod("get_" + propertyName, BindingFlags.Instance | BindingFlags.Public));

			return methodBuilder.GetILGenerator();
		}

		private ILGenerator CreateGeneratorForReflectionEmittedMethod(string methodName)
		{
			MethodAttributes methodAttributes;
			var explicitInterfaceImplementation = false;
			var methodInfo = typeBuilder.BaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
			
			if (methodInfo == null)
			{
				explicitInterfaceImplementation = true;
				methodInfo = GetBaseType(typeBuilder).GetMethod(typeof(IDataAccessObject).FullName + "." + methodName, BindingFlags.Instance | BindingFlags.NonPublic);

				methodAttributes = methodInfo.Attributes | MethodAttributes.NewSlot;
			}
			else
			{
				methodAttributes = methodInfo.Attributes &  ~(MethodAttributes.Abstract | MethodAttributes.NewSlot);
			}

			var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.CallingConvention, methodInfo.ReturnType, methodInfo.GetParameters().Convert(c => c.ParameterType).ToArray());

			if (explicitInterfaceImplementation)
			{
				typeBuilder.DefineMethodOverride(methodBuilder, typeof(IDataAccessObject).GetMethod(methodName));
			}

			return methodBuilder.GetILGenerator();
		}
	}
}
