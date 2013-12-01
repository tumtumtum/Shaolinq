// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public struct AssemblyBuildInfo
	{
		public readonly Assembly concreteAssembly;
		public readonly Assembly definitionAssembly;

		private readonly Dictionary<Type, Type> enumerableTypes;
		private readonly Dictionary<Type, Type> typesByConcreteType;
		private readonly Dictionary<Type, Type> concreteTypesByType;
		private readonly Dictionary<Type, Type> dataAccessObjectsTypes;
		private readonly Dictionary<Type, MethodInfo> whereMethodInfos;
		private readonly Dictionary<Type, Type> modelTypesByConcreteModelType;
		private readonly Dictionary<Type, Type> concreteModelTypesByModelType;
		private readonly Dictionary<Type, Delegate> dataAccessModelConstructors;
		private readonly Dictionary<Type, Delegate> dataAccessObjectConstructors;
		private readonly Dictionary<Type, string> persistenceContextNamesByConcreteType;
		private readonly Dictionary<Type, Dictionary<Type, string>> queryablePersistenceContextNamesByConcreteModelType;
		
		public AssemblyBuildInfo(Assembly concreteAssembly, Assembly definitionAssembly)
		{
			this.concreteAssembly = concreteAssembly;
			this.definitionAssembly = definitionAssembly;
			this.dataAccessModelConstructors = new Dictionary<Type, Delegate>(PrimeNumbers.Prime7);
			this.typesByConcreteType = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.concreteTypesByType = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.modelTypesByConcreteModelType = new Dictionary<Type, Type>(PrimeNumbers.Prime7);
			this.concreteModelTypesByModelType = new Dictionary<Type, Type>(PrimeNumbers.Prime7);
			this.dataAccessObjectConstructors = new Dictionary<Type, Delegate>(PrimeNumbers.Prime67);
			this.persistenceContextNamesByConcreteType = new Dictionary<Type, string>(PrimeNumbers.Prime7);
			this.queryablePersistenceContextNamesByConcreteModelType = new Dictionary<Type, Dictionary<Type, string>>(PrimeNumbers.Prime7);
			this.whereMethodInfos = new Dictionary<Type, MethodInfo>(PrimeNumbers.Prime127);
			this.dataAccessObjectsTypes = new Dictionary<Type, Type>(PrimeNumbers.Prime127);
			this.enumerableTypes = new Dictionary<Type, Type>(PrimeNumbers.Prime127);

			var typeProvider = TypeDescriptorProvider.GetProvider(definitionAssembly);

			foreach (var type in typeProvider.GetTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(type.Type.Namespace + "." + type.Type.Name);

				this.concreteTypesByType[type.Type] = concreteType;
				this.typesByConcreteType[concreteType] = type.Type;
				persistenceContextNamesByConcreteType[concreteType] = type.PersistenceContextName;

				this.whereMethodInfos[type.Type] = MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(type.Type);
				this.dataAccessObjectsTypes[type.Type] = TypeHelper.DataAccessObjectsType.MakeGenericType(type.Type);
				this.enumerableTypes[type.Type] = TypeHelper.IEnumerableType.MakeGenericType(type.Type);
			}

			foreach (var descriptor in typeProvider.GetModelTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(descriptor.Type.Namespace + "." + descriptor.Type.Name);

				this.concreteModelTypesByModelType[descriptor.Type] = concreteType;
				this.modelTypesByConcreteModelType[concreteType] = descriptor.Type;

				var modelPersistenceContextNamesByConcreteType = new Dictionary<Type, string>(PrimeNumbers.Prime127);

				foreach (var queryableType in descriptor.GetQueryableTypes())
				{
					Type concreteQueryableType;

					if (queryableType.Assembly == this.concreteAssembly || queryableType.Assembly == this.definitionAssembly)
					{
						concreteQueryableType = this.concreteTypesByType[queryableType];
					}
					else
					{
						concreteQueryableType = DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(queryableType.Assembly).concreteTypesByType[queryableType];
					}

					modelPersistenceContextNamesByConcreteType[concreteQueryableType] = descriptor.GetQueryablePersistenceContextName(queryableType);
				}

				this.queryablePersistenceContextNamesByConcreteModelType[concreteType] = modelPersistenceContextNamesByConcreteType;
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
      
		public T NewDataAccessModel<T>()
			where T : DataAccessModel
		{
			Delegate constructor;
			
			if (!this.dataAccessModelConstructors.TryGetValue(typeof(T), out constructor))
			{
				Type type;

				if (typeof(T).Assembly == this.concreteAssembly || typeof(T).Assembly == this.definitionAssembly)
				{
					if (!this.concreteModelTypesByModelType.TryGetValue(typeof(T), out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for {0}", typeof(T));
					}
				}
				else
				{
					var b = DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(typeof(T).Assembly);

					type = b.concreteTypesByType[typeof(T)];
				}

				constructor = Expression.Lambda(Expression.Convert(Expression.New(type), typeof(T))).Compile();

				this.dataAccessModelConstructors[typeof(T)] = constructor;
			}

			return ((Func<T>)constructor)();
		}

		public IDataAccessObject NewDataAccessObject(Type dataAccessObjectType)
		{
			Delegate constructor;

			if (!this.dataAccessObjectConstructors.TryGetValue(dataAccessObjectType, out constructor))
			{
				Type type;
                
				if (dataAccessObjectType.Assembly == this.concreteAssembly || dataAccessObjectType.Assembly == this.definitionAssembly)
				{
					if (!this.concreteTypesByType.TryGetValue(dataAccessObjectType, out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for {0}", dataAccessObjectType);
					}
				}
				else
				{
					var b = DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(dataAccessObjectType.Assembly);

					type = b.concreteTypesByType[dataAccessObjectType];
				}

				constructor = Expression.Lambda(Expression.Convert(Expression.New(type), dataAccessObjectType)).Compile();

				this.dataAccessObjectConstructors[dataAccessObjectType] = constructor;
			}

			return (IDataAccessObject)constructor.DynamicInvoke();
		}

		public T NewDataAccessObject<T>()
			where T : IDataAccessObject
		{
			Delegate constructor;

			if (!this.dataAccessObjectConstructors.TryGetValue(typeof(T), out constructor))
			{
				Type type;

				if (typeof(T).Assembly == this.concreteAssembly || typeof(T).Assembly == this.definitionAssembly)
				{
					if (!this.concreteTypesByType.TryGetValue(typeof(T), out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for DataAccessObject type: {0}", type);
					}
				}
				else
				{
					var b = DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(typeof(T).Assembly);

					type = b.concreteTypesByType[typeof(T)];
				}

				constructor = Expression.Lambda(Expression.Convert(Expression.New(type), typeof(T))).Compile();

				this.dataAccessObjectConstructors[typeof(T)] = constructor;
			}

			return ((Func<T>)constructor)();
		}

		public IDictionary<Type, string> GetQueryablePersistenceContextNamesByModelType(Type modelType)
		{
			return this.queryablePersistenceContextNamesByConcreteModelType[modelType];
		}
		
		public IDictionary<Type, string> GetPersistenceContextNamesByConcreteType()
		{
			return this.persistenceContextNamesByConcreteType;
		}

		public string GetPersistenceContextName(Type definitionType)
		{
			return this.persistenceContextNamesByConcreteType[definitionType];
		}

		public Type GetConcreteType(Type definitionType)
		{
			Type retval;

			if (this.concreteAssembly.Equals(definitionType.Assembly))
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

			if (this.definitionAssembly.Equals(concreteType.Assembly))
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

			if (this.definitionAssembly.Equals(concreteModelType.Assembly))
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

			if (this.concreteAssembly.Equals(definitionModelType.Assembly))
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
