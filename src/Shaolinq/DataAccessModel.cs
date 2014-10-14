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
		public virtual event EventHandler Disposed;

		public Assembly DefinitionAssembly { get; private set; }
		public AssemblyBuildInfo AssemblyBuildInfo { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public ModelTypeDescriptor ModelTypeDescriptor { get; private set; }
		private readonly Dictionary<string, SqlDatabaseContextsInfo> sqlDatabaseContextsByCategory = new Dictionary<string, SqlDatabaseContextsInfo>(StringComparer.InvariantCultureIgnoreCase);
		private Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>> inflateFuncsByType = new Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>>();
		private readonly Dictionary<Type, Func<Object, ObjectPropertyValue[]>> propertyInfoAndValueGetterFuncByType = new Dictionary<Type, Func<object, ObjectPropertyValue[]>>();
		internal DataAccessObjectProjectionContext DataAccessObjectProjectionContext { get; private set; }
		internal RelatedDataAccessObjectsInitializeActionsCache relatedDataAccessObjectsInitializeActionsCache = new RelatedDataAccessObjectsInitializeActionsCache();

		[ReflectionEmitted]
		protected abstract void Initialise();

		public virtual DataAccessObjects<T> GetDataAccessObjects<T>()
			where T : class, IDataAccessObject
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

		private struct SqlDatabaseContextsInfo
		{
			public uint counter;
			public List<SqlDatabaseContext> databaseContexts;

			public static SqlDatabaseContextsInfo Create()
			{
				return new SqlDatabaseContextsInfo()
				{
					databaseContexts = new List<SqlDatabaseContext>()
				};
			}
		}
		
		public DataAccessModelTransactionManager AmbientTransactionManager
		{
			get
			{
				if (this.disposed == 1)
				{
					throw new ObjectDisposedException(this.GetType().Name);
				}

				return DataAccessModelTransactionManager.GetAmbientTransactionManager(this);
			}
		}

		protected DataAccessModel()
		{
			this.DataAccessObjectProjectionContext = new DataAccessObjectProjectionContext(this);
		}

		protected virtual void OnDisposed(EventArgs eventArgs)
		{
			if (this.Disposed != null)
			{
				this.Disposed(this, eventArgs);
			}
		}

		~DataAccessModel()
		{
			Dispose();
		}

		private int disposed = 0;

		public virtual void Dispose()
		{
			this.DisposeAllSqlDatabaseContexts();

			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				this.DisposeAllSqlDatabaseContexts();

				this.OnDisposed(EventArgs.Empty);

				GC.SuppressFinalize(this);
			}
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

					info.databaseContexts.Add(newSqlDatabaseContext);
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

						info.databaseContexts.Add(newSqlDatabaseContext);
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

		/// <summary>
		/// Flushes and closes all connections for the DataAccessModel associated with the current thread.
		/// </summary>
		private void DisposeAllSqlDatabaseContexts()
		{
			DataAccessModelTransactionManager.GetAmbientTransactionManager(this).FlushConnections();

			foreach (var key in this.sqlDatabaseContextsByCategory.Keys)
			{
				foreach (var context in this.sqlDatabaseContextsByCategory[key].databaseContexts)
				{
					context.Dispose();
				}
			}
		}

		public virtual T GetReferenceByPrimaryKey<T>(ObjectPropertyValue[] primaryKey)
			where T : class, IDataAccessObject
		{
			foreach (var keyValue in primaryKey)
			{
				if (keyValue.Value == null)
				{
					return null;
				}
			}

			var propertyInfoAndValues = primaryKey;

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetConcreteTypeFromDefinitionType(typeof(T)), propertyInfoAndValues);

			if (existing != null)
			{
				return (T)existing;
			}
			else
			{
				var retval = this.AssemblyBuildInfo.CreateDataAccessObject<T>(this, false);

				retval.SetIsDeflatedReference(true);
				retval.SetPrimaryKeys(propertyInfoAndValues);
				retval.ResetModified();
				retval.FinishedInitializing();
				retval.SubmitToCache();

				return retval;
			}
		}

		public virtual T GetReferenceByPrimaryKey<T, K>(K primaryKey)
			where T : DataAccessObject<K>
		{
			return this.GetReferenceByPrimaryKey<T>(new { Id = primaryKey });
		}

		private class ContainerType<T>
		{	
		}

		public virtual T GetReferenceByPrimaryKey<T>(object[] primaryKeyValues)
			where T : class, IDataAccessObject
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException("primaryKeyValues");
			}

			if (primaryKeyValues.All(c => c == null))
			{
				return null;
			}

			var objectType = typeof(ContainerType<T>);
			Func<object, ObjectPropertyValue[]> func;

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

				propertyInfoAndValueGetterFuncByType[objectType] = func;
			}

			var propertyInfoAndValues = func(primaryKeyValues);

			return this.GetReferenceByPrimaryKey<T>(propertyInfoAndValues);
		}

		public virtual T GetReferenceByPrimaryKey<T>(object primaryKey)
			where T : class, IDataAccessObject
		{
			if (primaryKey == null)
			{
				throw new ArgumentNullException("primaryKey");
			}

			var objectType = primaryKey.GetType();
			Func<object, ObjectPropertyValue[]> func;

			if (!propertyInfoAndValueGetterFuncByType.TryGetValue(objectType, out func))
			{
				var isSimpleType = TypeDescriptor.IsSimpleType(objectType);
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

				if (isSimpleType && typeDescriptor.PrimaryKeyCount != 1)
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

					if (isSimpleType)
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

				propertyInfoAndValueGetterFuncByType[objectType] = func;
			}

			var propertyInfoAndValues = func(primaryKey); 
			
			return this.GetReferenceByPrimaryKey<T>(propertyInfoAndValues);
		}

		public virtual IDataAccessObject CreateDataAccessObject(Type type)
		{
			return this.CreateDataAccessObject(type, false);
		}

		public virtual T CreateDataAccessObject<T>()
			where T : class, IDataAccessObject
		{
			return this.CreateDataAccessObject<T>(false);
		}

		public virtual IDataAccessObject CreateDataAccessObject(Type type, bool transient)
		{
			var retval = this.AssemblyBuildInfo.CreateDataAccessObject(type, this, true);

			if (!transient)
			{
				retval.FinishedInitializing();
				retval.SubmitToCache();
			}
			else
			{
				retval.FinishedInitializing();
				retval.SetTransient(true);
			}

			return retval;
		}

		public virtual T CreateDataAccessObject<T>(bool transient)
			where T : class, IDataAccessObject
		{
			var retval = this.AssemblyBuildInfo.CreateDataAccessObject<T>(this, true);

			if (!transient)
			{
				retval.FinishedInitializing();
				retval.SubmitToCache();
			}
			else
			{
				retval.SetTransient(true);
			}

			return retval;
		}

		internal IPersistenceQueryProvider NewQueryProvider()
		{
			return this.GetCurrentSqlDatabaseContext().CreateQueryProvider();
		}

		public virtual SqlDatabaseContext GetCurrentSqlDatabaseContext()
		{
			var forWrite = Transaction.Current != null;

			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(forWrite);
			
			if (transactionContext.SqlDatabaseContext == null)
			{
				SqlDatabaseContextsInfo info;

				if (!this.sqlDatabaseContextsByCategory.TryGetValue(transactionContext.DatabaseContextCategoriesKey, out info))
				{
					var compositeInfo = SqlDatabaseContextsInfo.Create();

					foreach (var category in transactionContext.DatabaseContextCategories)
					{
						info = this.sqlDatabaseContextsByCategory[category];

						compositeInfo.databaseContexts.AddRange(info.databaseContexts);
					}

					info = this.sqlDatabaseContextsByCategory[transactionContext.DatabaseContextCategoriesKey] = compositeInfo;
				}

				var index = (int)(info.counter++ % info.databaseContexts.Count);

				transactionContext.SqlDatabaseContext = info.databaseContexts[index];
			}
			
			return transactionContext.SqlDatabaseContext;
		}

		public virtual void SetCurrentTransactionDatabaseCategories(params string[] categories)
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(false);
			
			if (transactionContext.DatabaseContextCategories == null)
			{
				foreach (var category in categories)
				{
					if (!this.sqlDatabaseContextsByCategory.ContainsKey(category))
					{
						throw new InvalidOperationException("Unsupported category: " + category);
					}
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

		public virtual IDataAccessObject Inflate(IDataAccessObject dataAccessObject)
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
				var methodInfo = this.GetType().GetMethods().First(c => c.Name == "Inflate" && c.IsGenericMethod);

				methodInfo = methodInfo.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType));

				var lambda = Expression.Lambda<Func<IDataAccessObject, IDataAccessObject>>(body, parameter);

				func = lambda.Compile();

				var newDictionary = new Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>>(inflateFuncsByType);

				newDictionary[definitionType] = func;

				inflateFuncsByType = newDictionary;
			}

			return func(dataAccessObject);
		}

		public virtual T Inflate<T>(T obj)
			where T : class, IDataAccessObject
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
