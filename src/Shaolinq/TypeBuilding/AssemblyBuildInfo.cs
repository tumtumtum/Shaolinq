// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public class AssemblyBuildInfo
	{
		private readonly Type dataAccessModelType;
		public Assembly ConcreteAssembly { get; private set; }
		public Assembly DefinitionAssembly { get; private set; }
		private readonly DataAccessModelConfiguration configuration;

		private readonly Dictionary<Type, Type> enumerableTypes;
		private readonly Dictionary<Type, Type> typesByConcreteType;
		private readonly Dictionary<Type, Type> concreteTypesByType;
		private readonly Dictionary<Type, Type> dataAccessObjectsTypes;
		private readonly Dictionary<Type, MethodInfo> whereMethodInfos;
		private readonly Dictionary<Type, Type> modelTypesByConcreteModelType;
		private readonly Dictionary<Type, Type> concreteModelTypesByModelType;
		private readonly Dictionary<Type, Delegate> dataAccessModelConstructors;
		private readonly Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>> dataAccessObjectConstructors;
		
		public AssemblyBuildInfo(Type dataAccessModelType, Assembly concreteAssembly, Assembly definitionAssembly, DataAccessModelConfiguration configuration)
		{
			Debug.Assert(dataAccessModelType.Assembly == definitionAssembly);

			this.dataAccessModelType = dataAccessModelType;
			this.ConcreteAssembly = concreteAssembly;
			this.DefinitionAssembly = definitionAssembly;
			this.configuration = configuration;
			this.dataAccessModelConstructors = new Dictionary<Type, Delegate>(PrimeNumbers.Prime7);
			this.typesByConcreteType = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.concreteTypesByType = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.modelTypesByConcreteModelType = new Dictionary<Type, Type>(PrimeNumbers.Prime7);
			this.concreteModelTypesByModelType = new Dictionary<Type, Type>(PrimeNumbers.Prime7);
			this.dataAccessObjectConstructors = new Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>>(PrimeNumbers.Prime67);
			this.whereMethodInfos = new Dictionary<Type, MethodInfo>(PrimeNumbers.Prime127);
			this.dataAccessObjectsTypes = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.enumerableTypes = new Dictionary<Type, Type>(PrimeNumbers.Prime127);

			var typeProvider = TypeDescriptorProvider.GetProvider(definitionAssembly);

			foreach (var type in typeProvider.GetTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(type.Type.Namespace + "." + type.Type.Name);

				this.concreteTypesByType[type.Type] = concreteType;
				this.typesByConcreteType[concreteType] = type.Type;
				
				this.whereMethodInfos[type.Type] = MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type.Type);
				this.dataAccessObjectsTypes[type.Type] = TypeHelper.DataAccessObjectsType.MakeGenericType(type.Type);
				this.enumerableTypes[type.Type] = TypeHelper.IEnumerableType.MakeGenericType(type.Type);
			}

			foreach (var descriptor in typeProvider.GetModelTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(descriptor.Type.Namespace + "." + descriptor.Type.Name);

				this.concreteModelTypesByModelType[descriptor.Type] = concreteType;
				this.modelTypesByConcreteModelType[concreteType] = descriptor.Type;
			}
		}

		public MethodInfo GetQueryableWhereMethod(Type type)
		{
			MethodInfo retval;

			if (this.whereMethodInfos.TryGetValue(type, out retval))
			{
				return retval;
			}

			return MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type);
		}

		public Type GetDataAccessObjectsType(Type type)
		{
			Type retval;
			
			if (this.dataAccessObjectsTypes.TryGetValue(type, out retval))
			{
				return retval;
			}

			return TypeHelper.DataAccessObjectsType.MakeGenericType(type);
		}

		public Type GetEnumerableType(Type type)
		{
			Type retval;

			if (this.enumerableTypes.TryGetValue(type, out retval))
			{
				return retval;
			}

			return TypeHelper.IEnumerableType.MakeGenericType(type);
		}

		public DataAccessModel NewDataAccessModel(Type dataAccessModelType)
		{
			var constructor = GetDataAccessModelConstructor(dataAccessModelType);

			return ((Func<DataAccessModel>)constructor)();
		}

		public T NewDataAccessModel<T>()
			where T : DataAccessModel
		{
			var constructor = GetDataAccessModelConstructor(typeof(T));

			return ((Func<T>)constructor)();
		}

		private Delegate GetDataAccessModelConstructor(Type dataAccessModelType)
		{
			Delegate constructor;

			if (!this.dataAccessModelConstructors.TryGetValue(dataAccessModelType, out constructor))
			{
				Type type;

				if (dataAccessModelType.Assembly == this.ConcreteAssembly || dataAccessModelType.Assembly == this.DefinitionAssembly)
				{
					if (!this.concreteModelTypesByModelType.TryGetValue(dataAccessModelType, out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for {0}", dataAccessModelType);
					}
				}
				else
				{
					throw new InvalidOperationException();
				}

				constructor = Expression.Lambda(Expression.Convert(Expression.New(type), dataAccessModelType)).Compile();

				this.dataAccessModelConstructors[dataAccessModelType] = constructor;
			}

			return constructor;
		}

		public DataAccessObject CreateDataAccessObject(Type dataAccessObjectType, DataAccessModel dataAccessModel, bool isNew)
		{
			Func<DataAccessModel, bool, DataAccessObject> constructor;

			if (!this.dataAccessObjectConstructors.TryGetValue(dataAccessObjectType, out constructor))
			{
				Type type;
                
				if (dataAccessObjectType.Assembly == this.ConcreteAssembly || dataAccessObjectType.Assembly == this.DefinitionAssembly)
				{
					if (!this.concreteTypesByType.TryGetValue(dataAccessObjectType, out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for {0}", dataAccessObjectType);
					}
				}
				else
				{
					throw new InvalidOperationException();
				}

				var isNewParam = Expression.Parameter(typeof(bool));
				var dataAccessModelParam = Expression.Parameter(typeof(DataAccessModel));

				constructor = Expression.Lambda<Func<DataAccessModel, bool, DataAccessObject>>(Expression.Convert(Expression.New(type.GetConstructor(new[] { typeof(DataAccessModel), typeof(bool) }), dataAccessModelParam, isNewParam), dataAccessObjectType), dataAccessModelParam, isNewParam).Compile();

				this.dataAccessObjectConstructors[dataAccessObjectType] = constructor;
			}

			return constructor(dataAccessModel, isNew);
		}

		public T CreateDataAccessObject<T>(DataAccessModel dataAccessModel, bool isNew)
			where T : DataAccessObject
		{
			return (T)CreateDataAccessObject(typeof(T), dataAccessModel, isNew);
		}

		public Type GetConcreteType(Type definitionType)
		{
			Type retval;

			if (this.ConcreteAssembly == definitionType.Assembly)
			{
				return definitionType;
			}

			if (this.concreteTypesByType.TryGetValue(definitionType, out retval))
			{
				return retval;
			}

			throw new ExpectedDataAccessObjectTypeException(definitionType);
		}

		public Type GetDefinitionType(Type concreteType)
		{
			Type retval;

			if (this.DefinitionAssembly == concreteType.Assembly)
			{
				return concreteType;
			}

			if (this.typesByConcreteType.TryGetValue(concreteType, out retval))
			{
				return retval;
			}

			throw new ExpectedDataAccessObjectTypeException(concreteType);
		}

		public Type GetDefinitionModelType(Type concreteModelType)
		{
			Type retval;

			if (this.DefinitionAssembly == concreteModelType.Assembly)
			{
				return concreteModelType;
			}

			if (this.modelTypesByConcreteModelType.TryGetValue(concreteModelType, out retval))
			{
				return retval;
			}

			throw new ExpectedDataAccessObjectTypeException(concreteModelType);
		}

		public Type GetConcreteModelType(Type definitionModelType)
		{
			Type retval;

			if (this.ConcreteAssembly == definitionModelType.Assembly)
			{
				return definitionModelType;
			}

			if (this.concreteModelTypesByModelType.TryGetValue(definitionModelType, out retval))
			{
				return retval;
			}

			throw new ExpectedDataAccessObjectTypeException(definitionModelType);
		}
	}
}
