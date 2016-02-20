// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public class RuntimeDataAccessModelInfo
	{
		private readonly Type dataAccessModelType;
		public TypeDescriptorProvider TypeDescriptorProvider { get; }
		public Assembly ConcreteAssembly { get; }
		public Assembly DefinitionAssembly { get; }
		
		private readonly Dictionary<Type, Type> typesByConcreteType = new Dictionary<Type, Type>();
		private readonly Dictionary<Type, Type> concreteTypesByType = new Dictionary<Type, Type>();
		private readonly Dictionary<Type, Type> dataAccessObjectsTypes = new Dictionary<Type, Type>();
		private Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>> dataAccessObjectConstructors = new Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>>();
		private readonly Func<DataAccessModel> dataAccessModelConstructor;
		
		public RuntimeDataAccessModelInfo(TypeDescriptorProvider typeDescriptorProvider, Assembly concreteAssembly, Assembly definitionAssembly)
		{
			this.TypeDescriptorProvider = typeDescriptorProvider;
			this.dataAccessModelType = typeDescriptorProvider.DataAccessModelType;
			
			Debug.Assert(this.dataAccessModelType.Assembly == definitionAssembly);

			this.ConcreteAssembly = concreteAssembly;
			this.DefinitionAssembly = definitionAssembly;

			var concreteDataAccessModelType = concreteAssembly.GetType(this.dataAccessModelType.Namespace + "." + this.dataAccessModelType.Name);
			this.dataAccessModelConstructor = Expression.Lambda<Func<DataAccessModel>>(Expression.Convert(Expression.New(concreteDataAccessModelType), this.dataAccessModelType)).Compile();

			foreach (var type in this.TypeDescriptorProvider.GetTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(type.Type.Namespace + "." + type.Type.Name);

				this.concreteTypesByType[type.Type] = concreteType;
				this.typesByConcreteType[concreteType] = type.Type;
				
				this.dataAccessObjectsTypes[type.Type] = TypeHelper.DataAccessObjectsType.MakeGenericType(type.Type);
			}
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

		public DataAccessModel NewDataAccessModel()
		{
			return this.dataAccessModelConstructor();
		}

		public DataAccessObject CreateDataAccessObject(Type dataAccessObjectType, DataAccessModel dataAccessModel, bool isNew)
		{
			return this.GetDataAccessObjectConstructor(dataAccessObjectType)(dataAccessModel, isNew);
		}

		private Func<DataAccessModel, bool, DataAccessObject> GetDataAccessObjectConstructor(Type dataAccessObjectType)
		{
			Func<DataAccessModel, bool, DataAccessObject> constructor;

			if (!this.dataAccessObjectConstructors.TryGetValue(dataAccessObjectType, out constructor))
			{
				Type type;
                
				if (!this.concreteTypesByType.TryGetValue(dataAccessObjectType, out type))
				{
					throw new InvalidDataAccessObjectModelDefinition("{0} it not part of {1}", dataAccessObjectType.Name, this.dataAccessModelType.Name);
				}

				var isNewParam = Expression.Parameter(typeof(bool));
				var dataAccessModelParam = Expression.Parameter(typeof(DataAccessModel));

				var constructorInfo = type.GetConstructor(new[] { typeof(DataAccessModel), typeof(bool) });
				
				if (constructorInfo == null)
				{
					throw new Exception($"Unexpected missing constructor on {type.Name}");
				}

				constructor = Expression.Lambda<Func<DataAccessModel, bool, DataAccessObject>>(Expression.Convert(Expression.New(constructorInfo, dataAccessModelParam, isNewParam), dataAccessObjectType), dataAccessModelParam, isNewParam).Compile();

				var newDataAccessObjectConstructors = new Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>>(this.dataAccessObjectConstructors) { [dataAccessObjectType] = constructor };
				
				this.dataAccessObjectConstructors = newDataAccessObjectConstructors;
			}

			return constructor;
		}

		public T CreateDataAccessObject<T>(DataAccessModel dataAccessModel, bool isNew)
			where T : DataAccessObject
		{
			return (T)this.GetDataAccessObjectConstructor(typeof(T))(dataAccessModel, isNew);
		}

		public Type GetConcreteType(Type definitionType)
		{
			Type retval;

			if (this.ConcreteAssembly == definitionType.Assembly)
			{
				return definitionType;
			}

			return this.concreteTypesByType.TryGetValue(definitionType, out retval) ? retval : definitionType;
		}

		public Type GetDefinitionType(Type concreteType)
		{
			Type retval;

			if (this.DefinitionAssembly == concreteType.Assembly)
			{
				return concreteType;
			}

			return this.typesByConcreteType.TryGetValue(concreteType, out retval) ? retval : concreteType;
		}
	}
}
