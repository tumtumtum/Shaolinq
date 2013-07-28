using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Shaolinq.Persistence;
using Platform;
using Platform.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessObjectTypeBuilder
	{
		internal static readonly string ForceSetPrefix = "ForceSet";
		internal static readonly string ObjectDataFieldName = "data";
		internal static readonly string HasChangedSuffix = "Changed";

		private readonly Type baseType;
		private TypeBuilder typeBuilder;
		private FieldInfo dataObjectField;
		private ILGenerator cctorGenerator;
		private FieldInfo isDeflatedReferenceField;
		private FieldBuilder partialObjectStateField;
		private TypeBuilder dataObjectTypeTypeBuilder;
		private ConstructorBuilder constructorBuilder;
		private ConstructorBuilder dataConstructorBuilder;
		private readonly TypeDescriptor typeDescriptor;
		private FieldInfo dataObjectChangedInDataObject;
		private readonly PersistenceContextAttribute persistenceContextAttribute;
		private readonly Dictionary<string, FieldBuilder> valueFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueChangedFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueIsSetFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> relatedIdFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, PropertyBuilder> propertyBuilders = new Dictionary<string, PropertyBuilder>();
		private readonly Dictionary<string, FieldInfo> propertyInfoFields = new Dictionary<string, FieldInfo>();
		private readonly Dictionary<string, MethodBuilder> setComputedValueMethods = new Dictionary<string, MethodBuilder>();

		public ModuleBuilder ModuleBuilder { get; private set; }
		public AssemblyBuildContext AssemblyBuildContext { get; private set; }

		public DataAccessObjectTypeBuilder(AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder, Type baseType)
		{
			this.baseType = baseType;
			this.ModuleBuilder = moduleBuilder;
			this.AssemblyBuildContext = assemblyBuildContext;

			assemblyBuildContext.TypeBuilders[baseType] = this;

			this.typeDescriptor = GetTypeDescriptor(baseType);

			persistenceContextAttribute = baseType.GetFirstCustomAttribute<PersistenceContextAttribute>(true)
			                              ?? PersistenceContextAttribute.Default;
		}

		private TypeDescriptor GetTypeDescriptor(Type type)
		{
			return TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(type);
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

				dataObjectChangedInDataObject = dataObjectTypeTypeBuilder.DefineField("HasObjectChanged", typeof(bool), FieldAttributes.Public);
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

						BuildPersistedProperty(propertyInfo, null, pass);

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
						this.BuildRelatedObjectHelperProperty(propertyInfo, EntityRelationshipType.OneToOne, pass);
						this.BuildRelatedObjectForeignKeysProperty(propertyInfo, pass);
					}
					else if (relatedObjectAttribute != null)
					{
						this.BuildRelatedObjectHelperProperty(propertyInfo, EntityRelationshipType.ChildOfOneToMany, pass);
						this.BuildRelatedObjectForeignKeysProperty(propertyInfo, pass);
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
				var secondConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(BaseDataAccessModel) });

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
				// Create default constructor

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
				}
			
				// Create type

				dataObjectTypeTypeBuilder.CreateType();

				this.ImplementDataAccessObjectMethods();
			}
		}

		private void ImplementDataAccessObjectMethods()
		{
			this.BuildHasObjectChangedProperty();
			this.BuildDataObjectProperty();
			this.BuildKeyTypeProperty();
			this.BuildNumberOfPrimaryKeysProperty();
			this.BuildNumberOfIntegerAutoIncrementPrimaryKeysProperty();
			this.BuildSetIntegerAutoIncrementValues();
			this.BuildCompositeKeyTypesProperty();
			this.BuildGetPrimaryKeysMethod();
			this.BuildSetPrimaryKeysMethod();
			this.BuildResetModified();
			this.BuildGetAllPropertiesMethod();
			this.BuildComputeIdRelatedComputedTextProperties();
			this.BuildGetChangedPropertiesMethod();
			this.BuildObjectStateProperty();
			this.BuildDefinesAnyAutoIncrementIntegerProperties();
			this.BuildSwapDataMethod();
			this.BuildHasPropertyChangedMethod();
			this.BuildSetIsNewMethod();
			this.BuildSetIsDeflatedReferenceMethod();
			this.BuildIsDeflatedReferenceProperty();
			this.BuildSetIsDeletedMethod();
			this.BuildGetHashCodeMethod();
			this.BuildEqualsMethod();
			this.BuildIsMissingAnyAutoIncrementIntegerPrimaryKeyValues();
			this.BuildSetAutoIncrementKeyValueMethod();
			this.BuildGetIntegerAutoIncrementPropertyInfosMethod();

			typeBuilder.CreateType();
		}

		protected virtual void BuildSetIntegerAutoIncrementValues()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIntegerAutoIncrementValues");

			var integerAutoIncrementProperties = typeDescriptor.PersistedProperties.Where(c => c.IsAutoIncrement && c.PropertyType.NonNullableType().IsIntegerType()).ToList();

			var i = 0;

			foreach (var propertyDescriptor in integerAutoIncrementProperties)
			{
				var propertyType = propertyDescriptor.PropertyType;
				
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelem, typeof(object));

				if (propertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, propertyType);
				}

				var prefix = "";

				if (propertyDescriptor.IsPrimaryKey)
				{
					prefix = ForceSetPrefix;
				}

				generator.Emit(OpCodes.Callvirt, this.propertyBuilders[prefix + propertyDescriptor.PropertyName].GetSetMethod());

				i++;
			}

			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetIntegerAutoIncrementPropertyInfosMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetIntegerAutoIncrementPropertyInfos");

			var integerAutoIncrementProperties = typeDescriptor.PersistedProperties.Where(c => c.IsAutoIncrement && c.PropertyType.NonNullableType().IsIntegerType()).ToList();

			var arrayLocal = generator.DeclareLocal(typeof(PropertyInfo[]));

			generator.Emit(OpCodes.Ldc_I4, integerAutoIncrementProperties.Count);
			generator.Emit(OpCodes.Newarr, typeof(PropertyInfo));
			generator.Emit(OpCodes.Stloc, arrayLocal);

			var i = 0;

			foreach (var propertyDescriptor in integerAutoIncrementProperties)
			{
				var fieldInfo = valueFields[propertyDescriptor.PropertyName];

				// Load array reference and index
				generator.Emit(OpCodes.Ldloc, arrayLocal);
				generator.Emit(OpCodes.Ldc_I4, i);

				// Load PropertyInfo
				generator.Emit(OpCodes.Ldsfld, this.propertyInfoFields[propertyDescriptor.PropertyInfo.Name]);

				// Store in array
				generator.Emit(OpCodes.Stelem, typeof(PropertyInfo));

				i++;
			}

			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildRelatedObjectForeignKeysProperty(PropertyInfo propertyInfo, int pass)
		{
			PropertyBuilder propertyBuilder;
			var propertyName = propertyInfo.Name + "ForeignKeys";
			var propertyType = typeof(PropertyInfoAndValue[]);

			if (pass == 1)
			{
				propertyBuilder = typeBuilder.DefineProperty(propertyName, propertyInfo.Attributes, propertyType, null, null, null, null, null);

				this.propertyBuilders[propertyName] = propertyBuilder;


				const MethodAttributes methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;

				var methodBuilder = typeBuilder.DefineMethod("get_" + propertyName, methodAttributes, CallingConventions.Standard, propertyType, Type.EmptyTypes);

				propertyBuilder.SetGetMethod(methodBuilder);
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyName];
				var propertyTypeDescriptor = TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(propertyInfo.PropertyType);

				var methodBuilder = (MethodBuilder)propertyBuilder.GetGetMethod();

				var generator = methodBuilder.GetILGenerator();

				var count = propertyTypeDescriptor.PrimaryKeyProperties.Count();
				var arrayLocal = generator.DeclareLocal(typeof(PropertyInfoAndValue[]));

				generator.Emit(OpCodes.Ldc_I4, count);
				generator.Emit(OpCodes.Newarr, arrayLocal.LocalType.GetElementType());
				generator.Emit(OpCodes.Stloc, arrayLocal);

				var i = 0;
				
				foreach (var propertyDescriptor in propertyTypeDescriptor.PrimaryKeyProperties)
				{
					var fieldInfo = valueFields[propertyInfo.Name + propertyDescriptor.PropertyInfo.Name];

					// Load array reference and index
					generator.Emit(OpCodes.Ldloc, arrayLocal);
					generator.Emit(OpCodes.Ldc_I4, i);

					// Load PropertyInfo
					generator.Emit(OpCodes.Ldsfld, this.propertyInfoFields[propertyInfo.Name + propertyDescriptor.PropertyInfo.Name]);
					
					// Load property value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, fieldInfo);

					if (fieldInfo.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, fieldInfo.FieldType);
					}

					// Load property name
					generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PropertyInfo.Name));

					// Load persisted name
					generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PersistedName));

					// Load is synthetic
					generator.Emit(OpCodes.Ldc_I4_0);

					// Load the property name hashcode
					generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.PropertyName.GetHashCode());

					generator.Emit(OpCodes.Newobj, PropertyInfoAndValueConstructor);

					// Store in array
					generator.Emit(OpCodes.Stelem, typeof(PropertyInfoAndValue));

					i++;
				}
			
				// Return array
				generator.Emit(OpCodes.Ldloc, arrayLocal);
				generator.Emit(OpCodes.Ret);
			}
		}

		private void BuildRelatedObjectHelperProperty(PropertyInfo propertyInfo, EntityRelationshipType relationshipType, int pass)
		{
			PropertyBuilder propertyBuilder;
			FieldBuilder currentFieldInDataObject; 
			var propertyName = propertyInfo.Name + "RelationshipHelperProperty";
			var propertyType = typeof(RelatedDataAccessObjects<>).MakeGenericType(propertyInfo.PropertyType);
			
			if (pass == 1)
			{
				propertyBuilder = typeBuilder.DefineProperty(propertyName, propertyInfo.Attributes, propertyType, null, null, null, null, null);

				var attributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				currentFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyName, propertyType, FieldAttributes.Public);
				currentFieldInDataObject.SetCustomAttribute(attributeBuilder);
				this.propertyBuilders[propertyName] = propertyBuilder;
				this.valueFields[propertyName] = currentFieldInDataObject;
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyName];
				currentFieldInDataObject = valueFields[propertyName];

				propertyBuilder.SetGetMethod(BuildRelatedDataAccessObjectsMethod(propertyName, MethodAttributes.Public, CallingConventions.Standard, propertyType, typeBuilder, dataObjectField, currentFieldInDataObject, propertyType.GetConstructor(Type.EmptyTypes), persistenceContextAttribute.GetPersistenceContextName(typeBuilder.BaseType), relationshipType, propertyInfo));
			}

			BuildPersistedProperty(propertyInfo, propertyBuilder, pass);
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

				propertyBuilder.SetGetMethod(BuildRelatedDataAccessObjectsMethod(propertyInfo.Name, propertyInfo.GetGetMethod().Attributes, propertyInfo.GetGetMethod().CallingConvention, propertyInfo.PropertyType, typeBuilder, dataObjectField, currentFieldInDataObject, currentFieldInDataObject.FieldType.GetConstructor(Type.EmptyTypes), persistenceContextAttribute.GetPersistenceContextName(typeBuilder.BaseType), EntityRelationshipType.ParentOfOneToMany, propertyInfo));
			}
		}

		protected virtual MethodBuilder BuildRelatedDataAccessObjectsMethod(string propertyName, MethodAttributes propertyAttributes, CallingConventions callingConventions, Type propertyType, TypeBuilder typeBuilder, FieldInfo dataObjectField, FieldInfo currentFieldInDataObject, ConstructorInfo constructorInfo, string persistenceContextName, EntityRelationshipType relationshipType, PropertyInfo propertyInfo)
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

			// Create RelatedDataAccessObjects
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
			generator.Emit(OpCodes.Callvirt, constructorInfo.DeclaringType.GetMethod("Initialize", new [] {typeof(IDataAccessObject), typeof(BaseDataAccessModel), typeof(EntityRelationshipType), typeof(string)}));

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

				var matches = ComputedTextMemberAttribute.FormatRegex.Matches(attribute.Format);

				var formatString = ComputedTextMemberAttribute.FormatRegex.Replace
				(
					attribute.Format, c =>
					                  {
											PropertyInfo pi;
											PropertyBuilder pb; 
											var name = c.Groups[1].Value;


											if (!propertyBuilders.TryGetValue(name, out pb))
											{
												pi = this.baseType.GetProperty(name);
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
					ilGenerator.Emit(OpCodes.Callvirt, componentPropertyInfo.GetGetMethod());
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

		private void BuildPersistedProperty(PropertyInfo propertyInfo, PropertyInfo relationshipProperty, int pass)
		{
			FieldBuilder currentFieldInDataObject;

			if (propertyInfo.PropertyType.IsDataAccessObjectType())
			{
				// For DataAccessObject property types we need to generate additional fields.
				// One field for each primary key in the referenced property.

				var propertyTypeTypeDescriptor = TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(propertyInfo.PropertyType);

				foreach (var propertyDescriptor in propertyTypeTypeDescriptor.PrimaryKeyProperties)
				{
					var innerPropertyType = propertyDescriptor.PropertyType;
					var name = propertyInfo.Name + propertyDescriptor.PersistedShortName;

					PropertyBuilder relatedIdPropertyBuilder;
					FieldBuilder relatedIdField, relatedValueIsSetFieldInDataObject;

					if (pass == 1)
					{
						relatedIdField = dataObjectTypeTypeBuilder.DefineField(name, innerPropertyType, FieldAttributes.Public);
						relatedIdFields[propertyInfo.Name + propertyDescriptor.PersistedShortName] = relatedIdField;

						relatedValueIsSetFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(name + "IsSet", typeof(bool), FieldAttributes.Public);
						var relatedValueIsSetAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
						relatedValueIsSetFieldInDataObject.SetCustomAttribute(relatedValueIsSetAttributeBuilder);
						valueIsSetFields.Add(name, relatedValueIsSetFieldInDataObject);
						valueFields[propertyInfo.Name + propertyDescriptor.PersistedShortName] = relatedIdField;

						relatedIdPropertyBuilder = typeBuilder.DefineProperty(name, propertyDescriptor.PropertyInfo.Attributes, innerPropertyType, null, null, null, null, null);
						propertyBuilders[name] = relatedIdPropertyBuilder;
                        
						// Create PropertyInfo cache
						var propertyInfoField = typeBuilder.DefineField("$$PropertyInfo_" + propertyInfo.Name + propertyDescriptor.PropertyName, typeof(PropertyInfo), FieldAttributes.Public | FieldAttributes.Static);
						cctorGenerator.Emit(OpCodes.Ldtoken, propertyTypeTypeDescriptor.Type);
						cctorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
						cctorGenerator.Emit(OpCodes.Ldstr, propertyDescriptor.PropertyName);
						cctorGenerator.Emit(OpCodes.Call, typeof(DataAccessObjectTypeBuilder).GetMethod("GetPropertyInfo", BindingFlags.Static | BindingFlags.Public));
						cctorGenerator.Emit(OpCodes.Stsfld, propertyInfoField);

						this.propertyInfoFields[propertyInfo.Name + propertyDescriptor.PropertyName] = propertyInfoField;
					}
					else
					{
						relatedIdField = relatedIdFields[propertyInfo.Name + propertyDescriptor.PersistedShortName];
						relatedValueIsSetFieldInDataObject = valueIsSetFields[name];
						relatedIdPropertyBuilder = propertyBuilders[name];
					}

					if (pass == 2)
					{
						relatedIdPropertyBuilder.SetSetMethod(BuildPropertyMethod(PropertyMethodType.Set, relatedIdPropertyBuilder.Name, propertyDescriptor.PropertyInfo, relatedIdPropertyBuilder, relatedIdField, null, null, true));
						relatedIdPropertyBuilder.SetGetMethod(BuildPropertyMethod(PropertyMethodType.Get, relatedIdPropertyBuilder.Name, propertyDescriptor.PropertyInfo, relatedIdPropertyBuilder, relatedIdField, null, null, true));
					}
				}
			}

			var propertyType = propertyInfo.PropertyType;
			PropertyBuilder propertyBuilder;
			FieldBuilder valueChangedFieldInDataObject, valueIsSetFieldInDataObject;

			if (pass == 1)
			{
				currentFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyType, FieldAttributes.Public);
				valueFields[propertyInfo.Name] = currentFieldInDataObject;

				valueChangedFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + DataAccessObjectTypeBuilder.HasChangedSuffix, typeof(bool), FieldAttributes.Public);
				valueChangedFields[propertyInfo.Name] = valueChangedFieldInDataObject;

				var valueChangedAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				valueChangedFieldInDataObject.SetCustomAttribute(valueChangedAttributeBuilder);

				valueIsSetFieldInDataObject = dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name + "IsSet", typeof(bool), FieldAttributes.Public);
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
				valueIsSetFieldInDataObject = valueIsSetFields[propertyInfo.Name];
				propertyBuilder = propertyBuilders[propertyInfo.Name];
			}
            
			if (pass == 1)
			{
				var propertyInfoField = typeBuilder.DefineField("$$PropertyInfo_" + propertyInfo.Name, typeof(PropertyInfo), FieldAttributes.Public | FieldAttributes.Static);
				
				cctorGenerator.Emit(OpCodes.Ldtoken, typeBuilder);
				cctorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
				cctorGenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
				cctorGenerator.Emit(OpCodes.Call, typeof(DataAccessObjectTypeBuilder).GetMethod("GetPropertyInfo", BindingFlags.Static | BindingFlags.Public));
				cctorGenerator.Emit(OpCodes.Stsfld, propertyInfoField);

				this.propertyInfoFields[propertyInfo.Name] = propertyInfoField;
			}

			if (pass == 2)
			{
				propertyBuilder.SetSetMethod(BuildPropertyMethod(PropertyMethodType.Set, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, relationshipProperty, false));
				propertyBuilder.SetGetMethod(BuildPropertyMethod(PropertyMethodType.Get, null, propertyInfo, propertyBuilder, currentFieldInDataObject, valueChangedFieldInDataObject, relationshipProperty, false));
			}
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
			if (operandType.IsPrimitive)
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

		/// <summary>
		/// Builds the getter or setter method for a property.
		/// </summary>
		/// <param name="propertyMethodType">Either "get" or "set"</param>
		/// <param name="propertyName">The name of the property to build (or null to use the propertyInfo name)</param>
		/// <param name="propertyInfo">The property whose get or set method to build</param>
		/// <param name="currentFieldInDataObject">The field inside the dataobject that stores the property's value</param>
		/// <param name="valueChangedFieldInDataObject">The field inside the dataobject that stores whether the property has changed</param>
		protected virtual MethodBuilder BuildPropertyMethod(PropertyMethodType propertyMethodType, string propertyName, PropertyInfo propertyInfo, PropertyBuilder propertyBuilder, FieldInfo currentFieldInDataObject, FieldInfo valueChangedFieldInDataObject, PropertyInfo relationshipProperty, bool isForiegnProperty)
		{
			if (propertyName == null)
			{
				propertyName = propertyInfo.Name;
			}

			Type returnType;
			Type[] parameters;

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
            
			var methodBuilder = typeBuilder.DefineMethod(propertyMethodType.ToString().ToLower() + "_" + propertyName, methodAttributes, CallingConventions.HasThis | CallingConventions.Standard, returnType, parameters);
			var generator = methodBuilder.GetILGenerator();
			var label = generator.DefineLabel();
			var currentPropertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyName);

			switch (propertyMethodType)
			{
				case PropertyMethodType.Get:
					generator.DeclareLocal(methodBuilder.ReturnType);

					if (currentPropertyDescriptor == null || !currentPropertyDescriptor.IsPrimaryKey)
					{
						// If the object is not write only then just return the value
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("IsDeflatedReference", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(true));
						generator.Emit(OpCodes.Brfalse, label);

						if (valueChangedFieldInDataObject != null)
						{
							// The object is write only so jump to return only if there is a value (value has changed)
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueChangedFieldInDataObject);
							generator.Emit(OpCodes.Brtrue, label);

							// Throw an exception
							//generator.Emit(OpCodes.Ldarg_0);
							//generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.WriteOnlyDataAccessObjectExceptionConstructor);
							//generator.Emit(OpCodes.Throw);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, this.typeDescriptor.Type.GetMethod("Inflate", BindingFlags.Instance | BindingFlags.Public));
						}
					}

					generator.MarkLabel(label);

					var returnLabel = generator.DefineLabel();
                    
					if (relationshipProperty != null)
					{
						var labelfrp = generator.DefineLabel();
						var local = generator.DeclareLocal(propertyInfo.PropertyType);

						// if (!this.data.PropertyIsSet)
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyInfo.Name]);
						generator.Emit(OpCodes.Brtrue, labelfrp);
						
						/*
						generator.BeginExceptionBlock();

						// local = this.PropertyRelationshipHelperProperty.First()
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, relationshipProperty.GetGetMethod());
						generator.Emit(OpCodes.Call, MethodInfoFastRef.EnumerableFirstMethod.MakeGenericMethod(propertyInfo.PropertyType));
						generator.Emit(OpCodes.Stloc, local);

						generator.BeginCatchBlock(typeof(InvalidOperationException));

						// SequenceEmptyException

						generator.Emit(OpCodes.Ldnull);
						generator.Emit(OpCodes.Stloc, local);
						
						generator.EndExceptionBlock();
						*/

						var propertyTypeDescriptor = TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(propertyInfo.PropertyType);

						var innerLabel = generator.DefineLabel();

						foreach (var propertyDescriptor in propertyTypeDescriptor.PrimaryKeyProperties)
						{
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyInfo.Name + propertyDescriptor.PersistedShortName]);
							generator.Emit(OpCodes.Brtrue, innerLabel);
						}

						generator.Emit(OpCodes.Ldnull);
						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(innerLabel);

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("DataAccessModel", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, propertyBuilders[propertyInfo.Name + "ForeignKeys"].GetGetMethod());
						generator.Emit(OpCodes.Callvirt, typeof(BaseDataAccessModel).GetMethod("GetReferenceByPrimaryKey", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(PropertyInfoAndValue[])}, null).MakeGenericMethod(propertyInfo.PropertyType));
						generator.Emit(OpCodes.Castclass, local.LocalType);
						generator.Emit(OpCodes.Stloc, local);

						// this.data.Property = local
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldloc, local);
						generator.Emit(OpCodes.Stfld, valueFields[propertyInfo.Name]);
						
						// this.PropertyIsSet = true
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldc_I4_1);
						generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyInfo.Name]);

						// return local
						generator.Emit(OpCodes.Ldloc, local);
						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(labelfrp);
					}

					if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
					{
						var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
						generator.Emit(OpCodes.Brtrue, returnLabel); 
                        
						// Create a new ShaolinqList and store in field

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Newobj, typeof(ShaolinqList<>).MakeGenericType(elementType).GetConstructor(Type.EmptyTypes));
						generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
					}
					else if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
					{
						var keyType = propertyInfo.PropertyType.GetGenericArguments()[0];
						var valueType = propertyInfo.PropertyType.GetGenericArguments()[1];

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
						generator.Emit(OpCodes.Brtrue, returnLabel);

						// Create a new ShaolinqList and store in field

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Newobj, typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType).GetConstructor(Type.EmptyTypes));
						generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
					}

					generator.MarkLabel(returnLabel);

					var pd = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyInfo.Name);

					var loadAndReturnLabel = generator.DefineLabel();

					if (pd.IsComputedTextMember)
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
						// If it's a Guid and not set then create a new Guid

						if (currentPropertyDescriptor.PropertyType.NonNullableType() == typeof(Guid))
						{
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyName]);
							generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuid);
							generator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyName].GetSetMethod());
						}
						else
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
					}

					generator.MarkLabel(loadAndReturnLabel);

					// Load value and return
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
					generator.Emit(OpCodes.Ret);

					break;
				case PropertyMethodType.Set:
					ILGenerator privateGenerator;
					var continueLabel = generator.DefineLabel();
					
					// Skip setting if value is the same as the previous value

					if ((propertyBuilder.PropertyType.IsPrimitive
						|| propertyBuilder.PropertyType == typeof(string)
						|| propertyBuilder.PropertyType == typeof(Guid)
						|| propertyBuilder.PropertyType == typeof(decimal))
						|| propertyBuilder.PropertyType.IsDataAccessObjectType())
					{
						if (!isForiegnProperty && propertyBuilder.PropertyType.IsDataAccessObjectType())
						{
							var innerLabel = generator.DefineLabel();
							var outerLabel = generator.DefineLabel();
							var hasChangedLocal = generator.DeclareLocal(typeof(bool));
							
							generator.Emit(OpCodes.Ldc_I4_0);
							generator.Emit(OpCodes.Stloc, hasChangedLocal);

							Debug.Assert(currentFieldInDataObject.FieldType.IsClass);

							// if (this.data.PropertyValue == null) skip
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
							generator.Emit(OpCodes.Brfalse, outerLabel);
                            
							// if (this.data.PropertyValue.IsNew) skip
							
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);
							generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternalIsNewProperty.GetGetMethod());
							generator.Emit(OpCodes.Brtrue, outerLabel);							

							var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[propertyBuilder.PropertyType];
							
							foreach (var referencedPropertyDescriptor in referencedTypeBuilder.typeDescriptor.PrimaryKeyProperties)
							{
								var referencedValueField = this.relatedIdFields[currentPropertyDescriptor.PersistedShortName + referencedPropertyDescriptor.PersistedShortName];
								var referencedObjectDataField = referencedTypeBuilder.dataObjectField;
								var referencedObjectValueField = referencedTypeBuilder.valueFields[referencedPropertyDescriptor.PropertyName];

								Debug.Assert(referencedValueField.FieldType == referencedObjectValueField.FieldType);

								// if (this.data.PropertyValueId != this.data.PropertyValue.Id)

								// Load this.data.PropertyValueId
								generator.Emit(OpCodes.Ldarg_0);
								generator.Emit(OpCodes.Ldfld, dataObjectField);
								generator.Emit(OpCodes.Ldfld, referencedValueField);

								// Load this.data.PropertyValue
								generator.Emit(OpCodes.Ldarg_0);
								generator.Emit(OpCodes.Ldfld, dataObjectField);
								generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);

								// Load this.data.PropertyValue.Id
								generator.Emit(OpCodes.Ldfld, referencedObjectDataField);
								generator.Emit(OpCodes.Ldfld, referencedObjectValueField);

								EmitCompareEquals(generator, referencedValueField.FieldType);

								generator.Emit(OpCodes.Brfalse, innerLabel);

								// if (value != null && this.data.PropertyValueId != value.Id)

								if (!propertyInfo.PropertyType.IsValueType)
								{
									generator.Emit(OpCodes.Ldarg_1);
									generator.Emit(OpCodes.Brfalse, innerLabel);
								}

								if (referencedPropertyDescriptor.IsAutoIncrement && referencedPropertyDescriptor.PropertyType.NonNullableType() != typeof(Guid))
								{
									// Don't access value.data.Id unless it's set (it's ok if it's a Guid cause they are created on first access)

									generator.Emit(OpCodes.Ldarg_1);
									generator.Emit(OpCodes.Ldfld, referencedObjectDataField);
									generator.Emit(OpCodes.Ldfld, referencedTypeBuilder.valueIsSetFields[referencedPropertyDescriptor.PropertyName]);
									generator.Emit(OpCodes.Brfalse, innerLabel);
								}

								// Load this.data.PropertyValueId
								generator.Emit(OpCodes.Ldarg_0);
								generator.Emit(OpCodes.Ldfld, dataObjectField);
								generator.Emit(OpCodes.Ldfld, referencedValueField);

								// Load value.PropertyValue
								generator.Emit(OpCodes.Ldarg_1);
								generator.Emit(OpCodes.Callvirt, referencedPropertyDescriptor.PropertyInfo.GetGetMethod());

								EmitCompareEquals(generator, referencedValueField.FieldType);
                                
								generator.Emit(OpCodes.Brfalse, innerLabel);


								generator.Emit(OpCodes.Ldc_I4_1);
								generator.Emit(OpCodes.Stloc, hasChangedLocal);

								generator.MarkLabel(innerLabel);
							}

							generator.MarkLabel(outerLabel);
							generator.Emit(OpCodes.Ldloc, hasChangedLocal);
						}
						else
						{
							// Load the new  value
							generator.Emit(OpCodes.Ldarg_1);

							// Load the old value
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, currentFieldInDataObject);

							// Compare and load true or false
							EmitCompareEquals(generator, propertyBuilder.PropertyType);
						}

						generator.Emit(OpCodes.Brfalse, continueLabel);
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldc_I4_1);
						generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);

						EmitUpdatedComputedPropertes(propertyBuilder.Name, generator);

						generator.Emit(OpCodes.Ret);
					}

					generator.MarkLabel(continueLabel);

					var storeInField = true;

					if (!isForiegnProperty)
					{
						if (propertyBuilder.PropertyType.IsGenericType && propertyBuilder.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
						{
							var elementType = currentPropertyDescriptor.PropertyType.GetGenericArguments()[0];
							var smartListType = typeof(ShaolinqList<>).MakeGenericType(elementType);

							var privatelabel1 = generator.DefineLabel();
							var privatelabel2 = generator.DefineLabel();

							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Isinst, smartListType);
							generator.Emit(OpCodes.Brfalse, privatelabel1);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
							generator.Emit(OpCodes.Br, privatelabel2);

							generator.MarkLabel(privatelabel1);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Newobj, smartListType.GetConstructor(new[] { currentPropertyDescriptor.PropertyType }));
							generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
							generator.MarkLabel(privatelabel2);

							storeInField = false;

							privateGenerator = generator;
						}
						else if (propertyBuilder.PropertyType.IsGenericType && propertyBuilder.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
						{
							var keyType = currentPropertyDescriptor.PropertyType.GetGenericArguments()[0];
							var valueType = currentPropertyDescriptor.PropertyType.GetGenericArguments()[1];
							var smartDictionaryType = typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType);

							var privatelabel1 = generator.DefineLabel();
							var privatelabel2 = generator.DefineLabel();

							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Isinst, smartDictionaryType);
							generator.Emit(OpCodes.Brfalse, privatelabel1);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
							generator.Emit(OpCodes.Br, privatelabel2);

							generator.MarkLabel(privatelabel1);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Newobj, ShoalinqDictionary.GetCopyConstructor(keyType, valueType));
							generator.Emit(OpCodes.Stfld, currentFieldInDataObject);
							generator.MarkLabel(privatelabel2);

							storeInField = false;

							privateGenerator = generator;
						}
						else if (currentPropertyDescriptor.IsAutoIncrement || currentPropertyDescriptor.IsPrimaryKey)
						{
							var skip1 = generator.DefineLabel();
							var skip2 = generator.DefineLabel();

							var propertyBuilder2 = typeBuilder.DefineProperty(ForceSetPrefix + propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, null, null, null, null, null);
							var privateSetMethodBuilder = typeBuilder.DefineMethod("set_" + ForceSetPrefix + propertyInfo.Name, methodAttributes, returnType, parameters);

							propertyBuilder2.SetSetMethod(privateSetMethodBuilder);
							privateGenerator = privateSetMethodBuilder.GetILGenerator();
							propertyBuilders[ForceSetPrefix + propertyInfo.Name] = propertyBuilder2;

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObject).GetProperty("IsTransient").GetGetMethod());
							generator.Emit(OpCodes.Brfalse, skip1);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyName].GetSetMethod());
							generator.Emit(OpCodes.Ret);

							generator.MarkLabel(skip1);

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldfld, dataObjectField);
							generator.Emit(OpCodes.Ldfld, valueIsSetFields[propertyName]);
							generator.Emit(OpCodes.Brtrue, skip2);
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldarg_1);
							generator.Emit(OpCodes.Callvirt, propertyBuilders[ForceSetPrefix + propertyName].GetSetMethod());
							generator.Emit(OpCodes.Ret);

							generator.MarkLabel(skip2);

							generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
							generator.Emit(OpCodes.Newobj, typeof(InvalidPrimaryKeyPropertyAccessException).GetConstructor(new[] { typeof(string) }));
							generator.Emit(OpCodes.Throw);

							generator.Emit(OpCodes.Ret);
						}
						else
						{
							privateGenerator = generator;
						}
					}
					else
					{
						privateGenerator = generator;
					}

					if (storeInField)
					{
						// Store value in field
						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
						privateGenerator.Emit(OpCodes.Ldarg_1);
						privateGenerator.Emit(OpCodes.Stfld, currentFieldInDataObject);
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
					privateGenerator.Emit(OpCodes.Ldc_I4_1);
					privateGenerator.Emit(OpCodes.Stfld, dataObjectChangedInDataObject);

					// Set value is set field
					privateGenerator.Emit(OpCodes.Ldarg_0);
					privateGenerator.Emit(OpCodes.Ldfld, dataObjectField);
					privateGenerator.Emit(OpCodes.Ldc_I4_1);
					privateGenerator.Emit(OpCodes.Stfld, valueIsSetFields[propertyName]);

					var computeLabel = privateGenerator.DefineLabel();
					var isNullLabel = privateGenerator.DefineLabel();
					
					if (!propertyInfo.PropertyType.IsValueType)
					{
						privateGenerator.Emit(OpCodes.Ldarg_1);
						privateGenerator.Emit(OpCodes.Brfalse, isNullLabel);
					}

					if (propertyInfo.PropertyType.IsDataAccessObjectType())
					{
						var innerTypeDescriptor = TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(propertyBuilder.PropertyType);
						var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[propertyBuilder.PropertyType];

						foreach (var propertyDescriptor in innerTypeDescriptor.PrimaryKeyProperties)
						{
							var nextPropertyLabel = privateGenerator.DefineLabel();
							var name = propertyInfo.Name + propertyDescriptor.PersistedShortName;
							var relatedIdField = relatedIdFields[name];

							if (propertyDescriptor.IsAutoIncrement && propertyDescriptor.PropertyType.NonNullableType() != typeof(Guid))
							{
								var referencedValueField = this.relatedIdFields[currentPropertyDescriptor.PersistedShortName + propertyDescriptor.PersistedShortName];
								var referencedObjectDataField = referencedTypeBuilder.dataObjectField;
								var referencedObjectValueIsSetField = referencedTypeBuilder.valueIsSetFields[propertyDescriptor.PropertyName];

								privateGenerator.Emit(OpCodes.Ldarg_1);
								privateGenerator.Emit(OpCodes.Ldfld, referencedObjectDataField);
								privateGenerator.Emit(OpCodes.Ldfld, referencedObjectValueIsSetField);
								privateGenerator.Emit(OpCodes.Brfalse, nextPropertyLabel);
							}

							privateGenerator.Emit(OpCodes.Ldarg_0);
							privateGenerator.Emit(OpCodes.Ldarg_1);
							privateGenerator.Emit(OpCodes.Callvirt, propertyDescriptor.PropertyInfo.GetGetMethod());
							privateGenerator.Emit(OpCodes.Callvirt, propertyBuilders[propertyInfo.Name + propertyDescriptor.PersistedShortName].GetSetMethod());
							privateGenerator.Emit(OpCodes.Br, nextPropertyLabel);

							privateGenerator.MarkLabel(nextPropertyLabel);
						}
					}

					privateGenerator.Emit(OpCodes.Br, computeLabel);
					
					privateGenerator.MarkLabel(isNullLabel);

					if (propertyInfo.PropertyType.IsDataAccessObjectType())
					{
						var innerTypeDescriptor = TypeDescriptorProvider.GetProvider(this.AssemblyBuildContext.SourceAssembly).GetTypeDescriptor(propertyBuilder.PropertyType);
						var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[propertyBuilder.PropertyType];

						foreach (var propertyDescriptor in innerTypeDescriptor.PrimaryKeyProperties)
						{
							privateGenerator.Emit(OpCodes.Ldarg_0);

							if (propertyDescriptor.PropertyType.IsValueType)
							{
								var valueLocal = privateGenerator.DeclareLocal(propertyDescriptor.PropertyType);

								privateGenerator.Emit(OpCodes.Ldloca, valueLocal);
								privateGenerator.Emit(OpCodes.Initobj, valueLocal.LocalType);
								privateGenerator.Emit(OpCodes.Ldloc, valueLocal);
							}
							else
							{
								privateGenerator.Emit(OpCodes.Ldnull);
							}

							privateGenerator.Emit(OpCodes.Callvirt, propertyBuilders[propertyInfo.Name + propertyDescriptor.PersistedShortName].GetSetMethod());
						}
					}

					privateGenerator.MarkLabel(computeLabel);

					EmitUpdatedComputedPropertes(propertyBuilder.Name, privateGenerator);

					privateGenerator.Emit(OpCodes.Ret);

					break;
			}

			return methodBuilder;
		}

		private void EmitUpdatedComputedPropertes(string propertyName, ILGenerator generator)
		{
			var propertyNames = new List<string>();

			foreach (var propertyDescriptor in typeDescriptor.ComputedTextProperties)
			{
				if (Enumerable.Contains(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences(), propertyName))
				{
					propertyNames.Add(propertyDescriptor.PropertyName);
				}
			}

			foreach (var name in propertyNames)
			{
				var methodInfo = setComputedValueMethods[name];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, methodInfo);
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

				if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
				{
					var retlabel = generator.DefineLabel();
					var elementType = property.PropertyType.GetGenericArguments()[0];
					var smartListType = typeof(ShaolinqList<>).MakeGenericType(elementType);

					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueFields[property.PropertyName]);
					generator.Emit(OpCodes.Castclass, smartListType);
					generator.Emit(OpCodes.Callvirt, ShaolinqList.GetChangedProperty(elementType).GetGetMethod());
					generator.Emit(OpCodes.Stloc, local);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Brtrue, retlabel);
					
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueChangedFields[property.PropertyName]);
					generator.Emit(OpCodes.Stloc, local);

					generator.MarkLabel(retlabel);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Ret);
				}
				else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				{
					var retlabel = generator.DefineLabel();
					var keyType = property.PropertyType.GetGenericArguments()[0];
					var valueType = property.PropertyType.GetGenericArguments()[1];
					var smartDictionaryType = typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType);

					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stloc, local);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueFields[property.PropertyName]);
					generator.Emit(OpCodes.Castclass, smartDictionaryType);
					generator.Emit(OpCodes.Callvirt, ShoalinqDictionary.GetChangedProperty(keyType, valueType).GetGetMethod());
					generator.Emit(OpCodes.Stloc, local);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Brtrue, retlabel);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueChangedFields[property.PropertyName]);
					generator.Emit(OpCodes.Stloc, local);

					generator.MarkLabel(retlabel);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Ret);
				}
				else
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueChangedFields[property.PropertyName]);
					generator.Emit(OpCodes.Ret);
				}
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
				generator.Emit(OpCodes.Ldelema, typeof(PropertyInfoAndValue));

				generator.Emit(OpCodes.Ldobj, typeof(PropertyInfoAndValue));

				// Get the "value" field
				generator.Emit(OpCodes.Ldfld, FieldInfoFastRef.PropertyInfoAndValueValueField);

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

		protected virtual void BuildGetPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeys");

			var count = typeDescriptor.PrimaryKeyProperties.Count();
			var arrayLocal = generator.DeclareLocal(typeof(PropertyInfoAndValue[]));
			
			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, arrayLocal.LocalType.GetElementType());
			generator.Emit(OpCodes.Stloc, arrayLocal);
            
			var i = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties)
			{
				var fieldInfo = valueFields[propertyDescriptor.PropertyInfo.Name];

				// Load array reference and index
				generator.Emit(OpCodes.Ldloc, arrayLocal);
				generator.Emit(OpCodes.Ldc_I4, i);

				// Load PropertyInfo
                generator.Emit(OpCodes.Ldsfld, this.propertyInfoFields[propertyDescriptor.PropertyInfo.Name]);
				
				// Load property value
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);

				if (fieldInfo.FieldType.IsValueType)
				{
					generator.Emit(OpCodes.Box, fieldInfo.FieldType);
				}
				
				// Load property name
				generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PropertyInfo.Name));
                
				// Load persisted name
				generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PersistedName));

				// Load is synthetic
				generator.Emit(OpCodes.Ldc_I4_0);

				// Load the property name hashcode
				generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.PropertyName.GetHashCode());

				generator.Emit(OpCodes.Newobj, PropertyInfoAndValueConstructor);

				// Store in array
				generator.Emit(OpCodes.Stelem, typeof(PropertyInfoAndValue));

				i++;
			}

			// Return array
			generator.Emit(OpCodes.Ldloc, arrayLocal);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildNumberOfIntegerAutoIncrementPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfIntegerAutoIncrementPrimaryKeys");

			generator.Emit(OpCodes.Ldc_I4, typeDescriptor.PrimaryKeyProperties.Count(c => c.IsAutoIncrement && c.PropertyType.NonNullableType().IsIntegerType()));
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

		protected virtual void BuildHasObjectChangedProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("HasObjectChanged");
            
			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				if (propertyDescriptor.PropertyType.IsGenericType && propertyDescriptor.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
				{
					var nextLabel = generator.DefineLabel();
					var elementType = propertyDescriptor.PropertyType.GetGenericArguments()[0];
					var smartListType = typeof(ShaolinqList<>).MakeGenericType(elementType);
					var field = valueFields[propertyDescriptor.PropertyName];

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Brfalse, nextLabel);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Castclass, smartListType);
					generator.Emit(OpCodes.Callvirt, ShaolinqList.GetChangedProperty(elementType).GetGetMethod());
					generator.Emit(OpCodes.Brfalse, nextLabel);

					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(nextLabel);
				}
				else if (propertyDescriptor.PropertyType.IsGenericType && propertyDescriptor.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				{
					var nextLabel = generator.DefineLabel();
					var keyType = propertyDescriptor.PropertyType.GetGenericArguments()[0];
					var valueType = propertyDescriptor.PropertyType.GetGenericArguments()[1];
					var smartListType = typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType);
					var field = valueFields[propertyDescriptor.PropertyName];

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Brfalse, nextLabel);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Castclass, smartListType);
					generator.Emit(OpCodes.Callvirt, ShoalinqDictionary.GetChangedProperty(keyType, valueType).GetGetMethod());
					generator.Emit(OpCodes.Brfalse, nextLabel);

					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(nextLabel);
				}
			}

			// Get and return changed field
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, dataObjectChangedInDataObject);

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

		protected virtual void BuildSetAutoIncrementKeyValueMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetAutoIncrementKeyValue");

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties.Filter(c => c.IsAutoIncrement))
			{
				var valueField = valueFields[propertyDescriptor.PropertyName];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				
				// Convert.ChangeType(value, typeof(PropertyType))
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldtoken, propertyDescriptor.PropertyType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.ConvertChangeTypeMethod);

				if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, propertyDescriptor.PropertyType);
				}
				else
				{
					generator.Emit(OpCodes.Castclass, propertyDescriptor.PropertyType);
				}

				// this.Id = Convert.ChangeType(value, typeof(PropertyType))
				generator.Emit(OpCodes.Stfld, valueField);

				// this.IdModified = true
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Stfld, valueChangedFields[propertyDescriptor.PropertyName]);

				// this.IsSet = true
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Stfld, valueIsSetFields[propertyDescriptor.PropertyName]);

				EmitUpdatedComputedPropertes(propertyDescriptor.PropertyName, generator);


				generator.Emit(OpCodes.Ret);

				return;
			}

			generator.Emit(OpCodes.Ldstr, "No autoincrement property defined on type: " + typeBuilder.Name);
			generator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new [] {typeof(string)}));
			generator.Emit(OpCodes.Throw);
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
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, dataObjectChangedInDataObject);
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
		protected virtual void BuildResetModified()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ResetModified");

			var local = generator.DeclareLocal(typeof(bool));
            
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc, local);

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Concat(this.typeDescriptor.RelatedProperties.Filter(c => c.IsBackReferenceProperty)))
			{
				if (propertyDescriptor.PropertyType.IsDataAccessObjectType() || propertyDescriptor.IsBackReferenceProperty)
				{
					var label = generator.DefineLabel();
					var label2 = generator.DefineLabel();
					var fieldInfo = valueFields[propertyDescriptor.PropertyName];
					var changedFieldInfo = valueChangedFields[propertyDescriptor.PropertyName];

					// PropertyValue == null
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, fieldInfo);
					generator.Emit(OpCodes.Brfalse, label);

					// PropertyValue.IsNew
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, fieldInfo);
					generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternalIsMissingAnyAutoIncrementIntegerPrimaryKeyValues.GetGetMethod());
					generator.Emit(OpCodes.Brfalse, label);

					// local = true
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Stloc, local);

					// PropertyValueModified = true
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Stfld, changedFieldInfo);

					generator.Emit(OpCodes.Br, label2);

					// PropertyValueModified = false
					generator.MarkLabel(label);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stfld, changedFieldInfo);

					generator.MarkLabel(label2);
				}
				else
				{
					var changedFieldInfo = valueChangedFields[propertyDescriptor.PropertyName];

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stfld, changedFieldInfo);
				}
			}

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Stfld, dataObjectChangedInDataObject);

			generator.Emit(OpCodes.Ret);
		}

		private static readonly ConstructorInfo PropertyInfoAndValueConstructor = typeof(PropertyInfoAndValue).GetConstructor(new[] { typeof(PropertyInfo), typeof(object), typeof(string), typeof(string), typeof(bool), typeof(int) });
		private static readonly MethodInfo PropertyInfoAndValueListAddMethod = typeof(List<PropertyInfoAndValue>).GetMethod("Add");
		private static readonly ConstructorInfo PropertyInfoAndValueListCtor = typeof(List<PropertyInfoAndValue>).GetConstructor(new [] { typeof(int) });
		
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

		protected virtual void BuildDataObjectProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("DataObject");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
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

		protected virtual void BuildComputeIdRelatedComputedTextProperties()
		{
			var count = 0; 
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ComputeIdRelatedComputedTextProperties");
			
			foreach (var propertyDescriptor in typeDescriptor.ComputedTextProperties)
			{
				if (Enumerable.Contains(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences(), "Id"))
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Callvirt, setComputedValueMethods[propertyDescriptor.PropertyName]);
				}

				count++;
			}

			generator.Emit(count > 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetAllPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetAllProperties");

			var listLocal = generator.DeclareLocal(typeof(List<PropertyInfoAndValue>));

			var count = this.typeDescriptor.PersistedProperties.Count;

			count -= this.typeDescriptor.PrimaryKeyProperties.Count(c => (c.IsAutoIncrement && c.PropertyType.IsIntegerType()));

			count += this.typeDescriptor.GetRelationshipInfos().Fold(c => c.RelatedTypeTypeDescriptor.PrimaryKeyCount, Operations.Add);

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newobj, PropertyInfoAndValueListCtor);
			generator.Emit(OpCodes.Stloc, listLocal);

			System.Action<PropertyDescriptor, FieldInfo, FieldInfo, bool, string, string> emitPropertyValue = (propertyDescriptor, valueField, nullCheckField, synthetic, persistedName, propertyName) =>
			{
				// Load list
				generator.Emit(OpCodes.Ldloc, listLocal);

				// Load PropertyInfo
				generator.Emit(OpCodes.Ldsfld, this.propertyInfoFields[propertyName]);

				if (nullCheckField == null)
				{
					// Load value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					if (valueField.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, valueField.FieldType);
					}
				}
				else
				{
					var label1 = generator.DefineLabel();
					var label2 = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, nullCheckField);
					generator.Emit(OpCodes.Brtrue, label1);

					// Load value
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Castclass, typeof(object));

					generator.Emit(OpCodes.Br, label2);

					generator.MarkLabel(label1);

					// Load value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					if (valueField.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, valueField.FieldType);
					}

					generator.Emit(OpCodes.Castclass, typeof(object));

					generator.MarkLabel(label2);
				}

				// Load property name
				generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PropertyName));

				// Load persisted name
				generator.Emit(OpCodes.Ldstr, String.Intern(persistedName));

				// Load is synthetic
				generator.Emit(synthetic ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

				// Load the property name hashcode
				generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.PropertyName.GetHashCode());

				// Construct the PropertyInfoValue
				generator.Emit(OpCodes.Newobj, PropertyInfoAndValueConstructor);

				// Call List.Add
				generator.Emit(OpCodes.Call, PropertyInfoAndValueListAddMethod);
			};

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Concat(this.typeDescriptor.RelatedProperties.Filter(c => c.BackReferenceAttribute != null)).OrderBy(c => c.PropertyName))
			{
				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var synthetic = propertyDescriptor.PropertyType.IsDataAccessObjectType();
				
				if (synthetic)
				{
					var referencedTypeDescriptor = GetTypeDescriptor(propertyDescriptor.PropertyType);

					emitPropertyValue(propertyDescriptor, valueField, valueField, false, propertyDescriptor.PersistedName, propertyDescriptor.PropertyName);

					foreach (var referencedPropertyDescriptor in referencedTypeDescriptor.PrimaryKeyProperties)
					{
						var referencedValueField = this.relatedIdFields[propertyDescriptor.PersistedShortName + referencedPropertyDescriptor.PersistedShortName];

						emitPropertyValue(referencedPropertyDescriptor, referencedValueField, valueField, true, propertyDescriptor.PropertyName + referencedPropertyDescriptor.PersistedShortName, propertyDescriptor.PropertyName + referencedPropertyDescriptor.PropertyName);
					}
				}
				else
				{
					emitPropertyValue(propertyDescriptor, valueField, null, false, propertyDescriptor.PersistedName, propertyDescriptor.PropertyName);
				}
			}

			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildGetChangedPropertiesMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetChangedProperties");

			var listLocal = generator.DeclareLocal(typeof(List<PropertyInfoAndValue>));

			var count = this.typeDescriptor.PersistedProperties.Count;

			count -= this.typeDescriptor.PrimaryKeyProperties.Count(c => (c.IsAutoIncrement && c.PropertyType.IsIntegerType()));

			count += this.typeDescriptor.GetRelationshipInfos().Fold(c => c.RelatedTypeTypeDescriptor.PrimaryKeyCount, Operations.Add);

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newobj, PropertyInfoAndValueListCtor);
			generator.Emit(OpCodes.Stloc, listLocal);

			System.Action<PropertyDescriptor, FieldInfo, FieldInfo, bool, string, string> emitPropertyValue = (propertyDescriptor, valueField, nullCheckField, synthetic, persistedName, propertyName) =>
    		{
				// Load list
				generator.Emit(OpCodes.Ldloc, listLocal);
				
				// Load PropertyInfo
				generator.Emit(OpCodes.Ldsfld, this.propertyInfoFields[propertyName]);

				if (nullCheckField == null)
				{
					// Load value
					generator.Emit(OpCodes.Ldarg_0);

					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					if (valueField.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, valueField.FieldType);
					}
				}
				else
				{
					var label1 = generator.DefineLabel();
					var label2 = generator.DefineLabel();

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, nullCheckField);
					generator.Emit(OpCodes.Brtrue, label1);
                    
					// Load value
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Castclass, typeof(object));

					generator.Emit(OpCodes.Br, label2);
					
					generator.MarkLabel(label1);

					// Load value

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					
					if (valueField.FieldType.IsValueType)
					{
						generator.Emit(OpCodes.Box, valueField.FieldType);
					}
					
					generator.Emit(OpCodes.Castclass, typeof(object));
                    
					generator.MarkLabel(label2);
				}

				// Load property name
    			generator.Emit(OpCodes.Ldstr, String.Intern(propertyDescriptor.PropertyName));

				// Load persisted name
    			generator.Emit(OpCodes.Ldstr, String.Intern(persistedName));
				
    			// Load is synthetic
				generator.Emit(synthetic ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

				// Load the property name hashcode
    			generator.Emit(OpCodes.Ldc_I4, propertyDescriptor.PropertyName.GetHashCode());

				// Construct the PropertyInfoValue
				generator.Emit(OpCodes.Newobj, PropertyInfoAndValueConstructor);

				// Call List.Add
				generator.Emit(OpCodes.Call, PropertyInfoAndValueListAddMethod);
    		};

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Concat(this.typeDescriptor.RelatedProperties.Filter(c => c.BackReferenceAttribute != null)))
			{
				if (propertyDescriptor.IsPrimaryKey && (propertyDescriptor.IsAutoIncrement && propertyDescriptor.PropertyType.IsIntegerType()))
				{ 
					continue;
				}

				var label = generator.DefineLabel();
				var valueField = this.valueFields[propertyDescriptor.PropertyName];
				var valueChangedField = this.valueChangedFields[propertyDescriptor.PropertyName];
				var synthetic = propertyDescriptor.PropertyType.IsDataAccessObjectType();
				var isRelatedObjectProperty = propertyDescriptor.IsBackReferenceProperty;

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, valueChangedField);

				if (synthetic || isRelatedObjectProperty)
				{
					var innerLabel = generator.DefineLabel();
					
					// if (this.data.PropertyValue != null)
					//   this.data.PropertyValueId = this.data.ProviderValue.data.Id

					// Load value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, innerLabel);

					// Update the current id properties with the one from the referenced object

					var referencedTypeDescriptor = GetTypeDescriptor(propertyDescriptor.PropertyType);
					var referencedTypeBuilder = this.AssemblyBuildContext.TypeBuilders[propertyDescriptor.PropertyType];

					foreach (var referencedPropertyDescriptor in referencedTypeDescriptor.PrimaryKeyProperties)
					{
						var innerInnerLabel = generator.DefineLabel();
						var referencedValueField = this.relatedIdFields[propertyDescriptor.PersistedShortName + referencedPropertyDescriptor.PersistedShortName];
						var referencedObjectDataField = referencedTypeBuilder.dataObjectField;
						var referencedObjectValueField = referencedTypeBuilder.valueFields[referencedPropertyDescriptor.PropertyName];
                        
						// if (this.data.PropertyValueId != this.data.PropertyValue.Id)

						// Load this.data.PropertyValueId
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, referencedValueField);

						// Load this.data.PropertyValue
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueField);

						
						// Load this.data.PropertyValue.Id
						//generator.Emit(OpCodes.Ldfld, referencedObjectDataField);
						//generator.Emit(OpCodes.Ldfld, referencedObjectValueField);
						generator.Emit(OpCodes.Callvirt, referencedTypeBuilder.propertyBuilders[referencedPropertyDescriptor.PropertyName].GetGetMethod());

						EmitCompareEquals(generator, referencedObjectValueField.FieldType);
                        
						generator.Emit(OpCodes.Brtrue, innerInnerLabel);
                        
						// Load this.data (for field storage later)
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);

						// Load this.data.PropertyValue
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, dataObjectField);
						generator.Emit(OpCodes.Ldfld, valueField);

						// Load this.data.PropertyValue.Id
						// generator.Emit(OpCodes.Ldfld, referencedObjectDataField);
						// generator.Emit(OpCodes.Ldfld, referencedObjectValueField);
						generator.Emit(OpCodes.Callvirt, referencedTypeBuilder.propertyBuilders[referencedPropertyDescriptor.PropertyName].GetGetMethod());

						// Store in field (this.data.PropertyValue.Id)
						generator.Emit(OpCodes.Stfld, referencedValueField);

						generator.MarkLabel(innerInnerLabel);
					}

					generator.Emit(OpCodes.Br, innerLabel);

					// PropertyValue == null
					generator.MarkLabel(innerLabel);
				}
                
				var label2 = generator.DefineLabel();

				generator.Emit(OpCodes.Brtrue, label2);

				var label3 = generator.DefineLabel();

				// Check if the list has changed
                if (propertyDescriptor.PropertyType.IsGenericType && propertyDescriptor.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
				{
					var elementType = propertyDescriptor.PropertyType.GetGenericArguments()[0];
                    
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, label3);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Castclass, typeof(ShaolinqList<>).MakeGenericType(elementType));
					generator.Emit(OpCodes.Callvirt, ShaolinqList.GetChangedProperty(elementType).GetGetMethod());
					generator.Emit(OpCodes.Brtrue, label2);
				}
				else if (propertyDescriptor.PropertyType.IsGenericType && propertyDescriptor.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				{
					var keyType = propertyDescriptor.PropertyType.GetGenericArguments()[0];
					var valueType = propertyDescriptor.PropertyType.GetGenericArguments()[1];

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, label3);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Castclass, typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType));
					generator.Emit(OpCodes.Callvirt, ShoalinqDictionary.GetChangedProperty(keyType, valueType).GetGetMethod());
					generator.Emit(OpCodes.Brtrue, label2);
				}
				
				generator.MarkLabel(label3);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, partialObjectStateField);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
				generator.Emit(OpCodes.Ceq);
				
				generator.Emit(OpCodes.Brfalse, label);

				generator.MarkLabel(label2);

				if (synthetic)
				{
					var referencedTypeDescriptor = GetTypeDescriptor(propertyDescriptor.PropertyType);
					
					foreach (var referencedPropertyDescriptor in referencedTypeDescriptor.PrimaryKeyProperties)
					{
						var referencedValueField = this.relatedIdFields[propertyDescriptor.PersistedShortName + referencedPropertyDescriptor.PersistedShortName];

						emitPropertyValue(referencedPropertyDescriptor, referencedValueField, valueField, true, propertyDescriptor.PropertyName + referencedPropertyDescriptor.PersistedShortName, propertyDescriptor.PropertyName + referencedPropertyDescriptor.PropertyName);
					}
				}
				else
				{
					emitPropertyValue(propertyDescriptor, valueField, null, false, propertyDescriptor.PersistedName, propertyDescriptor.PropertyName);
				}

				generator.MarkLabel(label);
			}
			
			generator.Emit(OpCodes.Ldloc, listLocal);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildIsMissingAnyAutoIncrementIntegerPrimaryKeyValues()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("IsMissingAnyAutoIncrementIntegerPrimaryKeyValues");

			foreach (var propertyDescriptor in this.typeDescriptor.PrimaryKeyProperties.Filter(c => c.IsAutoIncrement && c.PropertyType.NonNullableType().IsIntegerType()))
			{
				var valueField = valueFields[propertyDescriptor.PropertyName];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				
				if (propertyDescriptor.PropertyType.IsValueType)
				{
					switch (Type.GetTypeCode(propertyDescriptor.PropertyType))
					{
						case TypeCode.Int32:
							generator.Emit(OpCodes.Ldfld, valueField);
							generator.Emit(OpCodes.Ldc_I4_0);
							generator.Emit(OpCodes.Ceq);
							break;
						case TypeCode.Byte:
						case TypeCode.Int16:
							generator.Emit(OpCodes.Ldfld, valueField);
							generator.Emit(OpCodes.Ldc_I4_S, 0);
							generator.Emit(OpCodes.Ceq);
							break;
						case TypeCode.Int64:
							generator.Emit(OpCodes.Ldfld, valueField);
							generator.Emit(OpCodes.Ldc_I8, 0L);
							generator.Emit(OpCodes.Ceq);
							break;
						default:
							if (propertyDescriptor.PropertyType == typeof(Guid))
							{
								generator.Emit(OpCodes.Ldflda, valueField);
								generator.Emit(OpCodes.Ldsfld, FieldInfoFastRef.GuidEmptyGuid);
								generator.Emit(OpCodes.Call, MethodInfoFastRef.GuidEqualsMethod);
							}
							else
							{
								throw new NotSupportedException(propertyDescriptor.PropertyType.ToString());
							}
							break;
					}
				}
				else
				{
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Ceq);
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

		protected virtual void BuildDefinesAnyAutoIncrementIntegerProperties()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("DefinesAnyAutoIncrementIntegerProperties");

			if (this.typeDescriptor.PrimaryKeyProperties.Filter(c => c.IsAutoIncrement && c.PropertyType.NonNullableType().IsIntegerType()).Any())
			{
				generator.Emit(OpCodes.Ldc_I4_1);
				generator.Emit(OpCodes.Ret);

				return;
			}

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}

		protected virtual void BuildObjectStateProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("ObjectState");

			var label = generator.DefineLabel();
			var notDeletedLabel = generator.DefineLabel();
			var local = generator.DeclareLocal(typeof(ObjectState));

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, partialObjectStateField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brfalse, notDeletedLabel);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.Deleted);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(notDeletedLabel);

			// if (IsNew) local = ObjectState.NewChanged
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, dataObjectField);
			generator.Emit(OpCodes.Ldfld, partialObjectStateField);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.New);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.NewChanged);
			generator.Emit(OpCodes.Stloc, local);
			generator.MarkLabel(label);

			var breakLabel1 = generator.DefineLabel();

			// Go through foriegn keys properties and change local to include missing foreign
			// key flag if necessary

			foreach (var propertyDescriptor in this.typeDescriptor.RelatedProperties.Filter(c => c.IsBackReferenceProperty))
			{
				var innerLabel1 = generator.DefineLabel();
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (PropertyValue.IsNew) { retval |= MissingForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternalIsMissingAnyAutoIncrementIntegerPrimaryKeyValues.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.MissingForeignKeys);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, local);
				generator.Emit(OpCodes.Br, breakLabel1);

				generator.MarkLabel(innerLabel1);
			}

			generator.MarkLabel(breakLabel1);
			generator.Emit(OpCodes.Nop);
			generator.Emit(OpCodes.Nop);

			var breakLabel2 = generator.DefineLabel();

			// Go through persisted properties that are object references
			foreach (var propertyDescriptor in this.typeDescriptor.PersistedProperties.Filter(c => c.PropertyType.IsDataAccessObjectType()))
			{
				var innerLabel1 = generator.DefineLabel();
				var fieldInfo = this.valueFields[propertyDescriptor.PropertyName];

				// if (this.PropertyValue == null) { break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (PropertyValue.IsNew) { retval |= MissingUnconstrainedForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Callvirt, PropertyInfoFastRef.DataAccessObjectInternalIsMissingAnyAutoIncrementIntegerPrimaryKeyValues.GetGetMethod());
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				generator.Emit(OpCodes.Ldloc, local);
				generator.Emit(OpCodes.Ldc_I4, (int)ObjectState.MissingUnconstrainedForeignKeys);
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
