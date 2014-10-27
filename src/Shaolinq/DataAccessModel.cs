// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;
using Platform;

namespace Shaolinq
{
	public abstract class DataAccessModel
		 : MarshalByRefObject, IDisposable
	{
		private class RawPrimaryKeysPlaceholderType<T>
		{
		}

		private struct SqlDatabaseContextsInfo
		{
			public uint Count { get; set; }
			public List<SqlDatabaseContext> DatabaseContexts { get; private set; }

			public static SqlDatabaseContextsInfo Create()
			{
				return new SqlDatabaseContextsInfo
				{
					DatabaseContexts = new List<SqlDatabaseContext>()
				};
			}
		}

		public virtual event EventHandler Disposed;

		public Assembly DefinitionAssembly { get; private set; }
		public AssemblyBuildInfo AssemblyBuildInfo { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public ModelTypeDescriptor ModelTypeDescriptor { get; private set; }
		private readonly Dictionary<string, SqlDatabaseContextsInfo> sqlDatabaseContextsByCategory = new Dictionary<string, SqlDatabaseContextsInfo>(StringComparer.InvariantCultureIgnoreCase);
		private Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>> inflateFuncsByType = new Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>>();
		private Dictionary<Type, Func<Object, ObjectPropertyValue[]>> propertyInfoAndValueGetterFuncByType = new Dictionary<Type, Func<object, ObjectPropertyValue[]>>();
		internal RelatedDataAccessObjectsInitializeActionsCache relatedDataAccessObjectsInitializeActionsCache = new RelatedDataAccessObjectsInitializeActionsCache();

		[ReflectionEmitted]
		protected abstract void Initialise();

		public virtual DataAccessObjects<T> GetDataAccessObjects<T>()
			where T : DataAccessObject
		{
			return (DataAccessObjects<T>)GetDataAccessObjects(typeof(T));
		}

		[ReflectionEmitted]
		public abstract IQueryable GetDataAccessObjects(Type type);

		private Dictionary<Type, Func<IQueryable>> createDataAccessObjectsFuncs = new Dictionary<Type, Func<IQueryable>>();

		protected virtual IQueryable CreateDataAccessObjects(Type type)
		{
			Func<IQueryable> func;

			if (!createDataAccessObjectsFuncs.TryGetValue(type, out func))
			{
				var constructor = typeof(DataAccessObjects<>).MakeGenericType(type)
					.GetConstructor(new [] { typeof(DataAccessModel), typeof(Expression) });

				var body = Expression.Convert(Expression.New(constructor, Expression.Constant(this), Expression.Constant(null, typeof(Expression))), typeof(IQueryable));

				func = Expression.Lambda<Func<IQueryable>>(body).Compile();

				var newDictionary = new Dictionary<Type, Func<IQueryable>>(createDataAccessObjectsFuncs);

				newDictionary[type] = func;

				createDataAccessObjectsFuncs = newDictionary;
			}

			return func();
		}
		
		public DataAccessModelTransactionManager AmbientTransactionManager
		{
			get
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException(this.GetType().Name);
				}

				return DataAccessModelTransactionManager.GetAmbientTransactionManager(this);
			}
		}

		protected DataAccessModel()
		{
		}

		protected virtual void OnDisposed(EventArgs eventArgs)
		{

			var onDisposed = this.Disposed;

			if (onDisposed != null)
			{
				onDisposed(this, eventArgs);
			}
		}

		~DataAccessModel()
		{
			Dispose();
		}

		private void DisposeAllSqlDatabaseContexts()
		{
			DataAccessModelTransactionManager.GetAmbientTransactionManager(this).FlushConnections();

			foreach (var context in this.sqlDatabaseContextsByCategory
				.SelectMany(c => this.sqlDatabaseContextsByCategory.Values)
				.SelectMany(c => c.DatabaseContexts))
			{
				context.Dispose();
			}
		}

		private bool disposed;

		public virtual void Dispose()
		{
			if (disposed)
			{
				return;
			}

			this.disposed = true;
			this.DisposeAllSqlDatabaseContexts();
			this.OnDisposed(EventArgs.Empty);

			GC.SuppressFinalize(this);
		}

		internal Type GetConcreteTypeFromDefinitionType(Type definitionType)
		{
			return DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(definitionType.Assembly).GetConcreteType(definitionType);
		}

		internal Type GetDefinitionTypeFromConcreteType(Type concreteType)
		{
			return DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(concreteType.Assembly).GetDefinitionType(concreteType);
		}

		public virtual TypeDescriptor GetTypeDescriptor(Type type)
		{
			return this.TypeDescriptorProvider.GetTypeDescriptor(this.GetDefinitionTypeFromConcreteType(type));
		}

		public DataAccessObjectDataContext GetCurrentDataContext(bool forWrite)
		{
			return DataAccessModelTransactionManager.GetAmbientTransactionManager(this).GetCurrentContext(forWrite).CurrentDataContext;
		}

		public SqlTransactionalCommandsContext GetCurrentSqlDatabaseTransactionContext()
		{
			return DataAccessModelTransactionManager.GetAmbientTransactionManager(this).GetCurrentContext(true).GetCurrentDatabaseTransactionContext(this.GetCurrentSqlDatabaseContext());
		}

		private void SetAssemblyBuildInfo(AssemblyBuildInfo value)
		{
			this.AssemblyBuildInfo = value;
			this.DefinitionAssembly = value.definitionAssembly;
			this.TypeDescriptorProvider = TypeDescriptorProvider.GetProvider(this.DefinitionAssembly);
			this.ModelTypeDescriptor = this.TypeDescriptorProvider.GetModelTypeDescriptor(value.GetDefinitionModelType(this.GetType()));

			foreach (var contextInfo in this.Configuration.SqlDatabaseContextInfos)
			{
				SqlDatabaseContextsInfo info;
				var newSqlDatabaseContext = contextInfo.CreateSqlDatabaseContext(this);

				if (newSqlDatabaseContext.ContextCategories.Length == 0)
				{
					if (!this.sqlDatabaseContextsByCategory.TryGetValue(".", out info))
					{
						info = SqlDatabaseContextsInfo.Create();

						this.sqlDatabaseContextsByCategory["."] = info;
					}

					info.DatabaseContexts.Add(newSqlDatabaseContext);
				}
				else
				{
					foreach (var category in newSqlDatabaseContext.ContextCategories)
					{
						if (!this.sqlDatabaseContextsByCategory.TryGetValue(category, out info))
						{
							info = SqlDatabaseContextsInfo.Create();

							this.sqlDatabaseContextsByCategory[category] = info;
						}

						info.DatabaseContexts.Add(newSqlDatabaseContext);
					}
				}
			}

			if (!this.sqlDatabaseContextsByCategory.ContainsKey("."))
			{
				throw new InvalidDataAccessObjectModelDefinition("Configuration must define at least one root DatabaseContext category");
			}
		}

		public static DataAccessModel BuildDataAccessModel(Type dataAccessModelType)
		{
			return BuildDataAccessModel(dataAccessModelType, null);
		}

		public static DataAccessModel BuildDataAccessModel(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			if (!dataAccessModelType.IsSubclassOf(typeof (DataAccessModel)))
			{
				throw new ArgumentException("Data access model type must derive from DataAccessModel", "dataAccessModelType");
			}

			var builder = DataAccessModelAssemblyBuilder.Default;
			var buildInfo = builder.GetOrBuildConcreteAssembly(dataAccessModelType.Assembly);

			var retval = buildInfo.NewDataAccessModel(dataAccessModelType);

			retval.SetConfiguration(configuration);
			retval.SetAssemblyBuildInfo(buildInfo);

			retval.Initialise();

			return retval;
		}

		public static T BuildDataAccessModel<T>()
			where T : DataAccessModel
		{
			return BuildDataAccessModel<T>(null);
		}

		public static T BuildDataAccessModel<T>(DataAccessModelConfiguration configuration)
			where T : DataAccessModel
		{
			var builder = DataAccessModelAssemblyBuilder.Default;
			var buildInfo = builder.GetOrBuildConcreteAssembly(typeof(T).Assembly);

			var retval = buildInfo.NewDataAccessModel<T>();

			retval.SetConfiguration(configuration);
			retval.SetAssemblyBuildInfo(buildInfo);

			retval.Initialise();

			return retval;
		}

		public static DataAccessModelConfiguration GetConfiguration(string path)
		{
			return ConfigurationBlock<DataAccessModelConfiguration>.Load(path);
		}

		public DataAccessModelConfiguration GetDefaultConfiguration()
		{
			var typeName = this.GetType().Name;
			var configuration = DataAccessModel.GetConfiguration(typeName);

			if (configuration != null)
			{
				return configuration;
			}

			if (typeName.EndsWith("DataAccessModel"))
			{
				configuration = DataAccessModel.GetConfiguration(typeName.Left(typeName.Length - "DataAccessModel".Length));

				if (configuration != null)
				{
					return configuration;
				}
			}

			return null;
		}

		internal void SetConfiguration(DataAccessModelConfiguration configuration)
		{
			if (configuration == null)
			{
				configuration = GetDefaultConfiguration();

				if (configuration == null)
				{
					throw new InvalidOperationException("No configuration for: " + this.GetType().Name);
				}
			}

			this.Configuration = configuration;
		}


		protected internal virtual T GetReference<T>(ObjectPropertyValue[] primaryKey)
			where T : DataAccessObject
		{
			return (T)GetReference(typeof(T), primaryKey);
		}

		protected internal virtual IDataAccessObject GetReference<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			var primaryKeyValues = this.GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			return this.GetReference(type, primaryKeyValues);
		}

		protected internal virtual IDataAccessObject GetReference(Type type, ObjectPropertyValue[] primaryKey)
		{
			if (primaryKey == null)
			{
				return null;
			}

			if (primaryKey.Any(keyValue => keyValue.Value == null))
			{
				return null;
			}

			var objectPropertyAndValues = primaryKey;

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetConcreteTypeFromDefinitionType(type), objectPropertyAndValues);

			if (existing != null)
			{
				return existing;
			}
			else
			{
				var retval = (IDataAccessObject)this.AssemblyBuildInfo.CreateDataAccessObject(type, this, false);

				retval.SetIsDeflatedReference(true);
				retval.SetPrimaryKeys(objectPropertyAndValues);
				retval.ResetModified();
				retval.FinishedInitializing();
				retval.SubmitToCache();

				return retval;
			}
		}

		protected internal virtual T GetReference<T>(object[] primaryKeyValues)
			where T : DataAccessObject
		{
			var propertyValues = GetObjectPropertyValues<T>(primaryKeyValues);

			return this.GetReference<T>(propertyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValues<T>(object[] primaryKeyValues)
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException("primaryKeyValues");
			}

			if (primaryKeyValues.All(c => c == null))
			{
				return null;
			}

			Func<object, ObjectPropertyValue[]> func;
			var objectType = typeof(RawPrimaryKeysPlaceholderType<T>);

			if (!propertyInfoAndValueGetterFuncByType.TryGetValue(objectType, out func))
			{
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

				var parameter = Expression.Parameter(typeof(object));
				var constructor = ConstructorInfoFastRef.ObjectPropertyValueConstructor;

				var index = 0;
				var initializers = new List<Expression>();

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
					Expression convertedValue;

					var valueExpression = Expression.Convert(Expression.ArrayIndex(Expression.Convert(parameter, typeof(object[])), Expression.Constant(index)), typeof(object));
					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(this.GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

					if (property.PropertyType.IsDataAccessObjectType())
					{
						convertedValue = valueExpression;
					}
					else
					{
						convertedValue = Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, valueExpression, Expression.Constant(propertyInfo.PropertyType));
					}

					var newExpression = Expression.New(constructor, Expression.Constant(propertyInfo.PropertyType), Expression.Constant(property.PropertyName), Expression.Constant(property.PersistedName), Expression.Constant(property.PropertyName.GetHashCode()), convertedValue);

					initializers.Add(newExpression);
					index++;
				}

				var body = Expression.NewArrayInit(typeof(ObjectPropertyValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object, ObjectPropertyValue[]>), body, parameter);

				func = (Func<object, ObjectPropertyValue[]>)lambdaExpression.Compile();

				var newPropertyInfoAndValueGetterFuncByType = new Dictionary<Type, Func<object, ObjectPropertyValue[]>>(propertyInfoAndValueGetterFuncByType);
				newPropertyInfoAndValueGetterFuncByType[objectType] = func;

				propertyInfoAndValueGetterFuncByType = newPropertyInfoAndValueGetterFuncByType;
			}

			return func(primaryKeyValues);
		}

		protected  internal ObjectPropertyValue[] GetObjectPropertyValues<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (object.Equals(primaryKey, default(K)) && typeof(K).IsClass)
			{
				throw new ArgumentNullException("primaryKey");
			}

			var idType = primaryKey.GetType();
			var objectType = primaryKey.GetType();
			Func<object, ObjectPropertyValue[]> func;

			if (!propertyInfoAndValueGetterFuncByType.TryGetValue(objectType, out func))
			{
				var isSimpleKey = false;
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(type);
				var idPropertyType = typeDescriptor.GetPropertyDescriptorByPropertyName("Id").PropertyType;

				if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(idType) || (idType == idPropertyType && primaryKeyType == PrimaryKeyType.Auto))
				{
					isSimpleKey = true;
				}

				if (isSimpleKey && typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new InvalidOperationException("Composite primary key expected");
				}

				var parameter = Expression.Parameter(typeof(object));
				var constructor = ConstructorInfoFastRef.ObjectPropertyValueConstructor;

				var initializers = new List<Expression>();

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
					var isObjectType = property.PropertyType.IsDataAccessObjectType();

					Expression valueExpression;

					if (isSimpleKey)
					{
						valueExpression = parameter;
					}
					else
					{
						valueExpression = Expression.Convert(Expression.PropertyOrField(Expression.Convert(parameter, objectType), property.PropertyName), typeof(object));
					}

					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(this.GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

					var newExpression = Expression.New
					(
						constructor,
						Expression.Constant(propertyInfo.PropertyType),
						Expression.Constant(property.PropertyName),
						Expression.Constant(property.PersistedName),
						Expression.Constant(property.PropertyName.GetHashCode()),
						isObjectType ? (Expression)Expression.Convert(valueExpression, propertyInfo.PropertyType) : (Expression)Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, valueExpression, Expression.Constant(propertyInfo.PropertyType))
					);

					initializers.Add(newExpression);
				}

				var body = Expression.NewArrayInit(typeof(ObjectPropertyValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object, ObjectPropertyValue[]>), body, parameter);

				func = (Func<object, ObjectPropertyValue[]>)lambdaExpression.Compile();

				var newPropertyInfoAndValueGetterFuncByType = new Dictionary<Type, Func<object, ObjectPropertyValue[]>>(propertyInfoAndValueGetterFuncByType);
				newPropertyInfoAndValueGetterFuncByType[objectType] = func;

				propertyInfoAndValueGetterFuncByType = newPropertyInfoAndValueGetterFuncByType;
			}

			return func(primaryKey);
		}

		public virtual T GetReference<T, K>(K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
			where T : DataAccessObject
		{
			var propertyValues = GetObjectPropertyValues<K>(typeof(T), primaryKey, primaryKeyType);

			return this.GetReference<T>(propertyValues);
		}

		public virtual DataAccessObject CreateDataAccessObject(Type type)
		{
			var retval = this.AssemblyBuildInfo.CreateDataAccessObject(type, this, true);

			((IDataAccessObject)retval).FinishedInitializing();
			((IDataAccessObject)retval).SubmitToCache();

			return retval;
		}

		public virtual IDataAccessObject CreateDataAccessObject<K>(Type type, K primaryKey)
		{
			return CreateDataAccessObject(type, primaryKey, PrimaryKeyType.Auto);
		}

		public virtual IDataAccessObject CreateDataAccessObject<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			if (!typeof(IDataAccessObject).IsAssignableFrom(type)
				|| !typeof(DataAccessObject<>).IsAssignableFromIgnoreGenericParameters(type))
			{
				throw new ArgumentException("Type must be a DataAccessObjectType", "type");
			}

			var objectPropertyAndValues = GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			if (objectPropertyAndValues.Any(keyValue => keyValue.Value == null))
			{
				throw new MissingOrInvalidPrimaryKeyException();
			}

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetConcreteTypeFromDefinitionType(type), objectPropertyAndValues);

			if (existing != null)
			{
				IDataAccessObject obj = null;

				ActionUtils.IgnoreExceptions(() => obj = this.GetReference(type, primaryKey, primaryKeyType));

				throw new ObjectAlreadyExistsException(obj, null, "CreateDataAccessObject");
			}
			else
			{
				var retval = this.AssemblyBuildInfo.CreateDataAccessObject(type, this, true);

				((IDataAccessObject)retval).SetPrimaryKeys(objectPropertyAndValues);
				((IDataAccessObject)retval).FinishedInitializing();
				((IDataAccessObject)retval).SubmitToCache();

				return retval;
			}
		}

		public virtual T CreateDataAccessObject<T>()
			where T : DataAccessObject
		{
			var retval = this.AssemblyBuildInfo.CreateDataAccessObject<T>(this, true);

			((IDataAccessObject)retval).FinishedInitializing();
			((IDataAccessObject)retval).SubmitToCache();

			return retval;
		}

		public virtual T CreateDataAccessObject<T, K>(K primaryKey)
			where T : DataAccessObject
		{
			return CreateDataAccessObject<T, K>(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T CreateDataAccessObject<T, K>(K primaryKey, PrimaryKeyType primaryKeyType)
			where T : DataAccessObject
		{
			var objectPropertyAndValues = GetObjectPropertyValues<K>(typeof(T), primaryKey, primaryKeyType);

			if (objectPropertyAndValues.Any(keyValue => keyValue.Value == null))
			{
				throw new MissingOrInvalidPrimaryKeyException();
			}

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetConcreteTypeFromDefinitionType(typeof(T)), objectPropertyAndValues);

			if (existing != null)
			{
				T obj = null;

				ActionUtils.IgnoreExceptions(() => obj = this.GetReference<T, K>(primaryKey, primaryKeyType));

				throw new ObjectAlreadyExistsException(obj, null, "CreateDataAccessObject");
			}
			else
			{
				var retval = this.AssemblyBuildInfo.CreateDataAccessObject<T>(this, true);

				((IDataAccessObject)retval).SetPrimaryKeys(objectPropertyAndValues);
				((IDataAccessObject)retval).FinishedInitializing();
				((IDataAccessObject)retval).SubmitToCache();

				return retval;
			}
		}

		public virtual SqlDatabaseContext GetCurrentSqlDatabaseContext()
		{
			var forWrite = Transaction.Current != null;

			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(forWrite);

			if (transactionContext.SqlDatabaseContext != null)
			{
				return transactionContext.SqlDatabaseContext;
			}

			SqlDatabaseContextsInfo info;

			if (!this.sqlDatabaseContextsByCategory.TryGetValue(transactionContext.DatabaseContextCategoriesKey, out info))
			{
				var compositeInfo = SqlDatabaseContextsInfo.Create();

				foreach (var category in transactionContext.DatabaseContextCategories)
				{
					info = this.sqlDatabaseContextsByCategory[category];

					compositeInfo.DatabaseContexts.AddRange(info.DatabaseContexts);
				}

				info = this.sqlDatabaseContextsByCategory[transactionContext.DatabaseContextCategoriesKey] = compositeInfo;
			}

			var index = (int)(info.Count++ % info.DatabaseContexts.Count);

			transactionContext.SqlDatabaseContext = info.DatabaseContexts[index];

			return transactionContext.SqlDatabaseContext;
		}

		public virtual void SetCurrentTransactionDatabaseCategories(params string[] categories)
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(false);
			
			if (transactionContext.DatabaseContextCategories == null)
			{
				foreach (var category in categories
					.Where(category => !this.sqlDatabaseContextsByCategory.ContainsKey(category)))
				{
					throw new InvalidOperationException("Unsupported category: " + category);
				}

				transactionContext.DatabaseContextCategories = categories;
			}
			else
			{
				throw new InvalidOperationException("Transactions database context categories can only be set before any scope operations are performed");
			}
		}

		public virtual void SetCurentTransactionReadOnly()
		{
			this.SetCurrentTransactionDatabaseCategories("ReadOnly");
		}

		public virtual void CreateIfNotExist()
		{
			this.Create(DatabaseCreationOptions.IfNotExist);
		}

		public virtual void Create(DatabaseCreationOptions options)
		{
			using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				this.GetCurrentSqlDatabaseContext().SchemaManager.CreateDatabaseAndSchema((options & DatabaseCreationOptions.DeleteExisting) != 0);

				scope.Complete();
			}
		}

		public virtual void Flush()
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(true);

			this.GetCurrentDataContext(true).Commit(transactionContext, true);
		}

		protected internal IPersistenceQueryProvider NewQueryProvider()
		{
			return this.GetCurrentSqlDatabaseContext().CreateQueryProvider();
		}

		protected internal IDataAccessObject Inflate(IDataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException("dataAccessObject");
			}

			Func<IDataAccessObject, IDataAccessObject> func; 
			var definitionType = dataAccessObject.DefinitionType;
			
			if (!inflateFuncsByType.TryGetValue(definitionType, out func))
			{
				var parameter = Expression.Parameter(typeof(IDataAccessObject), "dataAccessObject");
				var methodInfo = MethodInfoFastRef.BaseDataAccessModelGenericInflateMethod.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType));

				var lambda = Expression.Lambda<Func<IDataAccessObject, IDataAccessObject>>(body, parameter);

				func = lambda.Compile();

				var newDictionary = new Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>>(inflateFuncsByType);

				newDictionary[definitionType] = func;

				inflateFuncsByType = newDictionary;
			}

			return func(dataAccessObject);
		}

		protected internal T Inflate<T>(T obj)
			where T : DataAccessObject
		{
			if (!obj.IsDeflatedReference)
			{
				return obj;
			}

			var retval = this.GetDataAccessObjects<T>().FirstOrDefault(c => c == obj);
			
			if (retval == null)
			{
				throw new MissingDataAccessObjectException(obj);
			}

			return retval;
		}
	}
}
