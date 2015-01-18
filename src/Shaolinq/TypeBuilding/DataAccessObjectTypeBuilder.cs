// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.TypeBuilding
{
	// ReSharper disable UnusedMember.Local

	public sealed class DataAccessObjectTypeBuilder
	{
		internal const string ForceSetPrefix = "$$force_set";
		internal const string IsSetSuffix = "$$is_set";
		internal const string HasChangedSuffix = "$$changed";
		internal const string DataObjectFieldName = "$$data";

		private static readonly Regex BuildMethodRegex = new Regex("^Build(.*)(Method|Property)$", RegexOptions.Compiled);

		public ModuleBuilder ModuleBuilder { get; private set; }
		public AssemblyBuildContext AssemblyBuildContext { get; private set; }

		private readonly Type baseType;
		private TypeBuilder typeBuilder;
		private FieldInfo dataObjectField;
		private ILGenerator cctorGenerator;
		private FieldInfo isDeflatedReferenceField;
		private FieldInfo originalPrimaryKeyField;
		private FieldInfo finishedInitializingField;
		private FieldInfo swappingField;
		private FieldBuilder isTransientField;
		private FieldBuilder partialObjectStateField;
		private TypeBuilder dataObjectTypeTypeBuilder;
		private TypeBuilder compositePrimaryKeyTypeBuilder;
		private readonly TypeDescriptor typeDescriptor;
		private ConstructorBuilder dataConstructorBuilder;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		
		private readonly Dictionary<string, FieldBuilder> valueFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueIsSetFields = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> valueChangedFields = new Dictionary<string, FieldBuilder>();
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

		private void BuildCompositePrimaryKeyType()
		{
			this.compositePrimaryKeyTypeBuilder = this.ModuleBuilder.DefineType(this.baseType.FullName + "PrimaryKey");
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

				// Define constructor for data object type

				this.dataConstructorBuilder = this.dataObjectTypeTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
				var constructorGenerator = this.dataConstructorBuilder.GetILGenerator();
				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));
				constructorGenerator.Emit(OpCodes.Ret);

				var attributeBuilder = new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

				this.dataObjectTypeTypeBuilder.SetCustomAttribute(attributeBuilder);
				this.partialObjectStateField = this.dataObjectTypeTypeBuilder.DefineField("PartialObjectState", typeof(ObjectState), FieldAttributes.Public);
				this.isTransientField = this.dataObjectTypeTypeBuilder.DefineField("IsTransient", typeof(bool), FieldAttributes.Public);

				this.isDeflatedReferenceField = this.dataObjectTypeTypeBuilder.DefineField("IsDeflatedReference", typeof(bool), FieldAttributes.Public);

				this.dataObjectField = this.typeBuilder.DefineField(DataObjectFieldName, this.dataObjectTypeTypeBuilder, FieldAttributes.Public);
			}
        
			var type = this.baseType;
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
						var propertyDescriptor = this.GetTypeDescriptor(this.baseType).GetPropertyDescriptorByPropertyName(propertyInfo.Name);

						if (propertyInfo.GetGetMethod() == null)
						{
							throw new InvalidDataAccessObjectModelDefinition("Type '{0}' defines a property '{1}' that is missing a get accessor", propertyInfo.Name, this.typeDescriptor.Type.Name);
						}

						if (propertyInfo.GetSetMethod() == null && !propertyDescriptor.IsComputedTextMember)
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
				constructorGenerator.Emit(OpCodes.Callvirt, this.baseType.GetMethod("SetDataAccessModel", BindingFlags.Instance | BindingFlags.NonPublic));

				constructorGenerator.Emit(OpCodes.Ldarg_0);
				constructorGenerator.Emit(OpCodes.Ldarg_2);
				constructorGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectInternal).GetMethod("SetIsNew"));

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
							constructorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuid);
							constructorGenerator.Emit(OpCodes.Call, propertyDescriptor.PropertyType.GetConstructor(new [] { typeof(Guid) }));
						}
						else
						{
							constructorGenerator.Emit(OpCodes.Call, MethodInfoFastRef.GuidNewGuid);
							constructorGenerator.Emit(OpCodes.Stloc, guidLocal);
						}

						constructorGenerator.Emit(OpCodes.Ldarg_0);
						constructorGenerator.Emit(OpCodes.Ldloc, guidLocal);
						constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyDescriptor.PropertyName].GetSetMethod());

						this.EmitUpdatedComputedPropertes(constructorGenerator, propertyDescriptor.PropertyName, propertyDescriptor.IsPrimaryKey);
					}
					else if (propertyDescriptor.PropertyType.IsValueType 
						&& Nullable.GetUnderlyingType(propertyDescriptor.PropertyType) == null 
						&& !propertyDescriptor.IsPrimaryKey
						&& !propertyDescriptor.IsAutoIncrement)
					{
						constructorGenerator.Emit(OpCodes.Ldarg_0);
						constructorGenerator.EmitDefaultValue(propertyDescriptor.PropertyType);
						constructorGenerator.Emit(OpCodes.Callvirt, this.propertyBuilders[propertyDescriptor.PropertyName].GetSetMethod());
					}
					else if (propertyDescriptor.PropertyType == typeof(string) && !propertyDescriptor.IsComputedTextMember)
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
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("PrimaryKeyIsCommitReady");

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
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("PrimaryKeyIsCommitReady").GetGetMethod());
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPropertiesGeneratedOnTheServerSide");

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
				this.propertyBuilders[propertyInfo.Name] = propertyBuilder;

				var attributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);

				currentFieldInDataObject = this.dataObjectTypeTypeBuilder.DefineField(propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Public);
				currentFieldInDataObject.SetCustomAttribute(attributeBuilder);

				this.valueFields[propertyInfo.Name] = currentFieldInDataObject;
			}
			else
			{
				propertyBuilder = this.propertyBuilders[propertyInfo.Name];
				currentFieldInDataObject = this.valueFields[propertyInfo.Name];

				propertyBuilder.SetGetMethod(this.BuildRelatedDataAccessObjectsMethod(propertyInfo.Name, propertyInfo.GetGetMethod().Attributes, propertyInfo.GetGetMethod().CallingConvention, propertyInfo.PropertyType, this.typeBuilder, this.dataObjectField, currentFieldInDataObject, EntityRelationshipType.ParentOfOneToMany, propertyInfo));
			}
		}

		private MethodBuilder BuildRelatedDataAccessObjectsMethod(string propertyName, MethodAttributes propertyAttributes, CallingConventions callingConventions, Type propertyType, TypeBuilder typeBuilder, FieldInfo dataObjectField, FieldInfo currentFieldInDataObject, EntityRelationshipType relationshipType, PropertyInfo propertyInfo)
		{
			var methodAttributes = MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (propertyAttributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Assembly | MethodAttributes.Family));
			var methodBuilder = typeBuilder.DefineMethod("get_" + propertyName, methodAttributes, callingConventions, propertyType, Type.EmptyTypes);
			var generator = methodBuilder.GetILGenerator();

			var constructor = currentFieldInDataObject.FieldType.GetConstructor(new [] { typeof(IDataAccessObjectAdvanced), typeof(DataAccessModel), typeof(EntityRelationshipType), typeof(string) });
    
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

			// Load "this"
			generator.Emit(OpCodes.Ldarg_0);

			// Load "this.DataAccessModel"
			generator.Emit(OpCodes.Ldarg_0);
			//generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetProperty("DataAccessModel", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());
			generator.Emit(OpCodes.Callvirt, typeBuilder.BaseType.GetMethod("GetDataAccessModel", BindingFlags.Instance | BindingFlags.Public));

			// Load relationship type
			generator.Emit(OpCodes.Ldc_I4, (int)relationshipType);

			// Load Property Name
			generator.Emit(OpCodes.Ldstr, propertyInfo.Name);

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

				var formatString = VariableSubstitutor.Substitute(attribute.Format, value =>
				{
					switch (value)
					{
						case "$(TABLENAME)":
						case "$(PERSISTEDTYPENAME)":
							return this.typeDescriptor.PersistedName;
						case "$(TABLENAME_LOWER)":
						case "$(PERSISTEDTYPENAME_LOWER)":
							return this.typeDescriptor.PersistedName.ToLower();
						case "$(TYPENAME)":
							return this.typeDescriptor.Type.Name;
						case "$(TYPENAME_LOWER)":
							return this.typeDescriptor.Type.Name.ToLower();
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

				var valueChangedAttributeBuilder = new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
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
			return object.Equals(left, right);
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
			var shouldBuildForceMethod = currentPropertyDescriptor.IsComputedTextMember;
			
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
					if (currentPropertyDescriptor == null || !currentPropertyDescriptor.IsPrimaryKey)
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
							generator.Emit(OpCodes.Callvirt, this.typeDescriptor.Type.GetMethod("Inflate", BindingFlags.Instance | BindingFlags.Public));
							generator.Emit(OpCodes.Pop);
						}
					}

					generator.MarkLabel(label);

					var propertyDescriptor = this.typeDescriptor.GetPropertyDescriptorByPropertyName(propertyInfo.Name);

					var loadAndReturnLabel = generator.DefineLabel();

					if (propertyDescriptor.IsComputedTextMember)
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

					if (currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey && currentPropertyDescriptor.IsAutoIncrement)
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, this.dataObjectField);
						generator.Emit(OpCodes.Ldfld, this.valueIsSetFields[propertyName]);
						generator.Emit(OpCodes.Brtrue, loadAndReturnLabel);

						// Not allowed to access primary key property if it's not set (not yet set by DB)

						generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						generator.Emit(OpCodes.Newobj, typeof(InvalidPrimaryKeyPropertyAccessException).GetConstructor(new[] { typeof(string) }));
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
					generator.Emit(OpCodes.Newobj, typeof(DeletedDataAccessObjectException).GetConstructor(new [] { typeof(IDataAccessObjectAdvanced) }));
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

						this.EmitUpdatedComputedPropertes(generator, propertyBuilder.Name, currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey);

						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(skipLabel);
					}

					if (shouldBuildForceMethod)
					{
						privateGenerator = forcePropertySetMethod.GetILGenerator();
						
						var skipThrowingLabel = generator.DefineLabel();

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsTransient").GetGetMethod());
						generator.Emit(OpCodes.Brtrue, skipThrowingLabel);

						generator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						generator.Emit(OpCodes.Newobj, typeof(InvalidPropertyAccessException).GetConstructor(new[] { typeof(string) }));
						generator.Emit(OpCodes.Throw);
						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(skipThrowingLabel);
						
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

						this.EmitUpdatedComputedPropertes(generator, propertyBuilder.Name, currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey);

						privateGenerator.Emit(OpCodes.Ret);
					}

					privateGenerator.MarkLabel(continueLabel);

					if (currentPropertyDescriptor.IsPrimaryKey)
					{
						var skipSaving = privateGenerator.DefineLabel();

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsNew").GetGetMethod());
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
						privateGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetMethod("GetPrimaryKeysFlattened"));
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
						privateGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("PrimaryKeyIsCommitReady").GetGetMethod());
						privateGenerator.Emit(OpCodes.Brfalse, skipCachingObjectLabel);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Ldfld, this.dataObjectField);
						privateGenerator.Emit(OpCodes.Ldfld, this.finishedInitializingField);
						privateGenerator.Emit(OpCodes.Brfalse, skipCachingObjectLabel);

						privateGenerator.Emit(OpCodes.Ldarg_0);
						privateGenerator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectInternal).GetMethod("SubmitToCache"));
						privateGenerator.Emit(OpCodes.Pop);
					}

					privateGenerator.MarkLabel(skipCachingObjectLabel);
					this.EmitUpdatedComputedPropertes(privateGenerator, propertyBuilder.Name, currentPropertyDescriptor != null && currentPropertyDescriptor.IsPrimaryKey);

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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("HasPropertyChanged");

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
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfPrimaryKeysGeneratedOnServerSide");

			generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide));
			generator.Emit(OpCodes.Ret);
		}

		private void BuildNumberOfPrimaryKeysProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("NumberOfPrimaryKeys");

			generator.Emit(OpCodes.Ldc_I4, this.typeDescriptor.PrimaryKeyCount);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildCompositeKeyTypesProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("CompositeKeyTypes");

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
					generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
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
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("KeyType");

			if (this.typeDescriptor.PrimaryKeyCount == 0)
			{
				generator.Emit(OpCodes.Ldnull);
			}
			else if (this.typeDescriptor.PrimaryKeyCount == 1)
			{
				generator.Emit(OpCodes.Ldtoken, this.typeDescriptor.PrimaryKeyProperties.First().PropertyType);
				generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}

			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetHashCodeAccountForServerGeneratedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetHashCodeAccountForServerGenerated");

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
						generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectInternal).GetMethod("GetHashCodeAccountForServerGenerated"));
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("MarkServerSidePropertiesAsApplied");
			
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
				generator.Emit(OpCodes.Ldc_I4, (int)(ObjectState.ServerSidePropertiesHydrated | ObjectState.ObjectInsertedWithinTransaction));
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("EqualsAccountForServerGenerated");

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
			generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys").GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnFalseLabel);

			generator.Emit(OpCodes.Ldloc, local);
			generator.Emit(OpCodes.Castclass, typeof(IDataAccessObjectInternal));
			generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys").GetGetMethod());
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

				//if (propertyDescriptor.PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Brfalse, returnLabel);
				}
				/*else
				{
					generator.Emit(OpCodes.Brtrue, label);

					// False if one of the values is null
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, returnLabel);
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);
					generator.Emit(OpCodes.Brfalse, returnLabel);

					// Use Object.Equals(object) method

					// Load our value
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					// Load operand value
					generator.Emit(OpCodes.Ldloc, local);
					generator.Emit(OpCodes.Ldfld, dataObjectField);
					generator.Emit(OpCodes.Ldfld, valueField);

					generator.Emit(OpCodes.Callvirt, MethodInfoFastRef.ObjectEqualsMethod);

					generator.Emit(OpCodes.Brfalse, returnLabel);
				}*/

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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SwapData");

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

				if (property.IsComputedTextMember)
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("FinishedInitializing");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Stfld, this.finishedInitializingField);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildResetModifiedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ResetModified");

			var returnLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsDeleted").GetGetMethod());
			generator.Emit(OpCodes.Brtrue, returnLabel);

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndRelatedObjectProperties)
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SubmitToCache");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, this.baseType.GetMethod("GetDataAccessModel", BindingFlags.Public | BindingFlags.Instance));
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Callvirt, typeof(DataAccessModel).GetMethod("GetCurrentDataContext", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(bool) }, null));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Callvirt, typeof(DataAccessObjectDataContext).GetMethod("CacheObject", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(DataAccessObject), typeof(bool) }, null));

			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsTransientMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsTransient");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Stfld, this.isTransientField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsNewMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsNew");

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
			generator.Emit(OpCodes.Ldc_I4, (int)0);
			generator.Emit(OpCodes.Stfld, this.partialObjectStateField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsDeflatedReferenceMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsDeflatedReference");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Stfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildCompoistePrimaryKeyProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("CompositePrimaryKey");

			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildIsDeflatedReferenceProperty()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter("IsDeflatedReference");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.dataObjectField);
			generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildSetIsDeletedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("SetIsDeleted");

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

			foreach (var propertyDescriptor in this.typeDescriptor.ComputedTextProperties)
			{
				foreach (var referencedPropertyName in this.GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences()))
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
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, this.isDeflatedReferenceField);
				generator.Emit(OpCodes.Brtrue, label);
			}

			foreach (var name in propertyNames)
			{
				var methodInfo = this.setComputedValueMethods[name];

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, methodInfo);
			}

			generator.MarkLabel(label);
		}

		private void BuildComputeServerGeneratedIdDependentComputedTextPropertiesMethod()
		{
			var count = 0;
			var generator = this.CreateGeneratorForReflectionEmittedMethod("ComputeServerGeneratedIdDependentComputedTextProperties");
			
			foreach (var propertyDescriptor in this.typeDescriptor.ComputedTextProperties)
			{
				var computedTextDependsOnAutoIncrementId = false;

				foreach (var propertyName in this.GetPropertyNamesAndDependentPropertyNames(propertyDescriptor.ComputedTextMemberAttribute.GetPropertyReferences()))
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
			generator.Emit(OpCodes.Call, MethodInfoFastRef.TypeGetTypeFromHandle);

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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetRelatedObjectProperties");

			var propertyDescriptors = this.typeDescriptor
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeys");

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

			// Return array
			generator.Emit(OpCodes.Ldloc, retval);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildGetPrimaryKeysFlattenedMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeysFlattened");
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetPrimaryKeysForUpdateFlattened");

			var label = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.originalPrimaryKeyField);
			generator.Emit(OpCodes.Brfalse, label);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, this.originalPrimaryKeyField);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(label);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetMethod("GetPrimaryKeysFlattened"));

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

				if (object.ReferenceEquals(visited, last))
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetAllProperties");
			var retval = generator.DeclareLocal(typeof(ObjectPropertyValue[]));

			var count = this.typeDescriptor.PersistedAndRelatedObjectProperties.Count;

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newarr, typeof(ObjectPropertyValue));
			generator.Emit(OpCodes.Stloc, retval);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndRelatedObjectProperties)
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod("GetChangedProperties");
			var count = this.typeDescriptor.PersistedProperties.Count + this.typeDescriptor.RelatedProperties.Count(c => c.BackReferenceAttribute != null);

			var listLocal = generator.DeclareLocal(typeof(List<ObjectPropertyValue>));

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Newobj, ConstructorInfoFastRef.ObjectPropertyValueListConstructor);
			generator.Emit(OpCodes.Stloc, listLocal);

			var index = 0;

			foreach (var propertyDescriptor in this.typeDescriptor.PersistedAndRelatedObjectProperties)
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
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name));
			
			var columnInfos = QueryBinder.GetPrimaryKeyColumnInfos(this.typeDescriptorProvider, this.typeDescriptor, (c, d) => true, (c, d) => c.IsPropertyThatIsCreatedOnTheServerSide);
		
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
			var generator = this.CreateGeneratorForReflectionEmittedMethod(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name));
			var properties = this.typeDescriptor.PersistedAndRelatedObjectProperties.ToList();

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
		
		private static string TrimCurrentMethodName(string methodName)
		{
			return BuildMethodRegex.Replace(methodName, c => c.Groups[1].Value);
		}

		private void BuildIsMissingAnyPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name));

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
					generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name)).GetGetMethod());
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

		private void BuildIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeysMethod()
		{
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name));

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
					generator.Emit(OpCodes.Callvirt, typeof(IDataAccessObjectAdvanced).GetProperty("IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys").GetGetMethod());
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
			var generator = this.CreateGeneratorForReflectionEmittedPropertyGetter(TrimCurrentMethodName(MethodBase.GetCurrentMethod().Name));

			var count = this.typeDescriptor.PersistedProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide);

			generator.Emit(OpCodes.Ldc_I4, count);
			generator.Emit(OpCodes.Ret);
		}

		private void BuildObjectStateProperty()
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
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (PropertyValue.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys)
				// { retval |= MissingConstrainedForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
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
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
				generator.Emit(OpCodes.Ldfld, fieldInfo);
				generator.Emit(OpCodes.Brfalse, innerLabel1);

				// if (this.PropertyValue.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys) { retval |= MissingUnconstrainedForeignKeys; break }
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, this.dataObjectField);
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
				methodInfo = typeof(IDataAccessObjectAdvanced).GetMethod(methodName);

				if (methodInfo == null)
				{
					methodInfo = typeof(IDataAccessObjectInternal).GetMethod(methodName);
				}

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

	// ReSharper restore UnusedMember.Local
}
