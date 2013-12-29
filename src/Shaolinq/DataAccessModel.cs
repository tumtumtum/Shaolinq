// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
 using System.Transactions;
 using Shaolinq.Persistence;
 using Shaolinq.Persistence.Sql;
 using Shaolinq.TypeBuilding;
using Platform;

namespace Shaolinq
{
	public abstract class DataAccessModel
		 : MarshalByRefObject, IDisposable
	{
		internal RelatedDataAccessObjectsInitializeActionsCache relatedDataAccessObjectsInitializeActionsCache = new RelatedDataAccessObjectsInitializeActionsCache();

		public Assembly DefinitionAssembly { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public virtual ModelTypeDescriptor ModelTypeDescriptor { get; private set; }

		[ReflectionEmitted]
		protected abstract void Initialise();

		[ReflectionEmitted]
		public abstract DataAccessObjects<T> GetDataAccessObjects<T>()
			where T : class, IDataAccessObject;

		public virtual TypeDescriptor GetTypeDescriptor(Type type)
		{
			return TypeDescriptorProvider.GetProvider(type.Assembly).GetTypeDescriptor(this.GetDefinitionTypeFromConcreteType(type));
		}

		public virtual U TranslateTo<U>(IDataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				return default(U);
			}

			var proxyFunction = ObjectToObjectProjectionProxyFunctionCache<U>.GetProxyFunction(dataAccessObject.GetType());

			return proxyFunction(this.DataAccessObjectProjectionContext, dataAccessObject);
		}

		public DataAccessObjectProjectionContext DataAccessObjectProjectionContext
		{
			get
			{
				if (this.dataAccessObjectProjectionContext == null)
				{
					var newInstance = new DataAccessObjectProjectionContext(this);

					Thread.MemoryBarrier();

					this.dataAccessObjectProjectionContext = newInstance;
				}

				return this.dataAccessObjectProjectionContext;
			}
		}
		private DataAccessObjectProjectionContext dataAccessObjectProjectionContext;

		public DatabaseConnection GetDatabaseConnection(IDataAccessObject dataAccessObject)
		{
			return this.GetDatabaseConnection(dataAccessObject.GetType());
		}

		public DatabaseConnection GetDatabaseConnection(Type type)
		{
			return this.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite);
		}

		#region Transaction Handling

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

		public virtual DataAccessObjectDataContext GetCurrentDataContext(bool forWrite)
		{
			return DataAccessModelTransactionManager.GetAmbientTransactionManager(this).GetCurrentContext(forWrite).CurrentDataContext;
		}

		#endregion

		#region BuildDataAccessModel

		public AssemblyBuildInfo AssemblyBuildInfo
		{
			get
			{
				return assemblyBuildInfo;
			}
		}

		private AssemblyBuildInfo assemblyBuildInfo;

		private readonly List<DatabaseConnection> databaseConnections = new List<DatabaseConnection>();

		private void SetAssemblyBuildInfo(AssemblyBuildInfo value)
		{
			this.assemblyBuildInfo = value;

			this.DefinitionAssembly = value.definitionAssembly;
			this.TypeDescriptorProvider = TypeDescriptorProvider.GetProvider(this.DefinitionAssembly);
			this.ModelTypeDescriptor = this.TypeDescriptorProvider.GetModelTypeDescriptor(value.GetDefinitionModelType(this.GetType()));

			foreach (var databaseConnectionInfo in this.Configuration.DatabaseConnectionInfos)
			{
				var newDatabaseConnection = databaseConnectionInfo.CreateDatabaseConnection();

				this.databaseConnections.Add(newDatabaseConnection);
			}
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

		public virtual DataAccessModelConfiguration GetConfiguration(string path)
		{
			return ConfigurationBlock<DataAccessModelConfiguration>.Load(path);
		}

		public DataAccessModelConfiguration GetDefaultConfiguration()
		{
			var typeName = this.GetType().Name;

			var configuration = this.GetConfiguration(typeName);

			if (configuration != null)
			{
				return configuration;
			}

			if (typeName.EndsWith("DataAccessModel"))
			{
				configuration = this.GetConfiguration(typeName.Left(typeName.Length - "DataAccessModel".Length));

				if (configuration != null)
				{
					return configuration;
				}
			}

			if (this.GetType().Name.EndsWith("DataAccessModel"))
			{
				var name = this.GetType().Name;

				configuration = this.GetConfiguration(name.Left(name.Length - "DataAccessModel".Length));

				if (configuration != null)
				{
					return configuration;
				}
			}

			if (!string.IsNullOrEmpty(this.GetType().Namespace))
			{
				var namespaceTail = this.GetType().Namespace.SplitAroundCharFromRight('.').Right;

				configuration = this.GetConfiguration(namespaceTail);

				if (configuration != null)
				{
					return configuration;
				}

				configuration = this.GetConfiguration(namespaceTail + "/DataAccessModel");

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

		public event EventHandler Disposed;

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

		/// <summary>
		/// Removes all cached connections for the database model associated with the current thread.
		/// </summary>
		public virtual void FlushConnections()
		{
			DataAccessModelTransactionManager.GetAmbientTransactionManager(this).FlushConnections();
			
			foreach (var context in this.databaseConnections)
			{
				context.DropAllConnections();
			}
		}

		public virtual void Dispose()
		{
			FlushConnections();

			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				FlushConnections();

				this.OnDisposed(EventArgs.Empty);

				GC.SuppressFinalize(this);
			}
		}

		#endregion

		#region Definition and Concrete Type Resolvers

		internal Type GetConcreteTypeFromDefinitionType(Type definitionType)
		{
			return DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(definitionType.Assembly).GetConcreteType(definitionType);
		}

		internal Type GetDefinitionTypeFromConcreteType(Type concreteType)
		{
			return DataAccessModelAssemblyBuilder.Default.GetOrBuildConcreteAssembly(concreteType.Assembly).GetDefinitionType(concreteType);
		}

		#endregion

		private readonly Dictionary<Type, Func<Object, PropertyInfoAndValue[]>> propertyInfoAndValueGetterFuncByType = new Dictionary<Type, Func<object, PropertyInfoAndValue[]>>();

		public virtual T GetReferenceByPrimaryKey<T>(PropertyInfoAndValue[] primaryKey)
			where T : class, IDataAccessObject
		{
			foreach (var keyValue in primaryKey)
			{
				if (keyValue.value == null)
				{
					return null;
				}
			}

			var propertyInfoAndValues = primaryKey;

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetDatabaseConnection(typeof(T)), this.GetConcreteTypeFromDefinitionType(typeof(T)), propertyInfoAndValues);

			if (existing != null)
			{
				return (T)existing;
			}
			else
			{
				var retval = this.assemblyBuildInfo.NewDataAccessObject<T>();

				retval.SetIsNew(false);
				retval.SetIsDeflatedReference(true);
				retval.SetDataAccessModel(this);
				retval.SetPrimaryKeys(propertyInfoAndValues);
				retval.ResetModified();

				this.GetCurrentDataContext(false).CacheObject(retval, false);

				return retval;
			}
		}

		public virtual T GetReferenceByPrimaryKey<T, K>(K primaryKey)
			where T : DataAccessObject<K>
		{
			return this.GetReferenceByPrimaryKey<T>(new { Id = primaryKey });
		}

		public virtual T GetReferenceByPrimaryKey<T>(object[] primaryKeyValues)
			where T : class, IDataAccessObject
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException("primaryKeyValues");
			}

			var objectType = typeof(object[]);
			Func<object, PropertyInfoAndValue[]> func;

			if (!propertyInfoAndValueGetterFuncByType.TryGetValue(objectType, out func))
			{
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

				var parameter = Expression.Parameter(typeof(object));
				var constructor = typeof(PropertyInfoAndValue).GetConstructor(new Type[] { typeof(PropertyInfo), typeof(object), typeof(string), typeof(string), typeof(bool), typeof(int) });

				var index = 0;
				var initializers = new List<Expression>();

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
					var valueExpression = Expression.Convert(Expression.ArrayIndex(Expression.Convert(parameter, typeof(object[])), Expression.Constant(index)), typeof(object));
					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(this.GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);
					var newExpression = Expression.New(constructor, Expression.Constant(propertyInfo), Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, valueExpression, Expression.Constant(propertyInfo.PropertyType)), Expression.Constant(property.PropertyName), Expression.Constant(property.PersistedName), Expression.Constant(false), Expression.Constant(property.PropertyName.GetHashCode()));

					initializers.Add(newExpression);
					index++;
				}

				var body = Expression.NewArrayInit(typeof(PropertyInfoAndValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object, PropertyInfoAndValue[]>), body, parameter);

				func = (Func<object, PropertyInfoAndValue[]>)lambdaExpression.Compile();

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
			Func<object, PropertyInfoAndValue[]> func;

			if (!propertyInfoAndValueGetterFuncByType.TryGetValue(objectType, out func))
			{
				var isSimpleType = TypeDescriptor.IsSimpleType(objectType);
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

				if (isSimpleType && typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new InvalidOperationException("Composite primary key expected");
				}

				var parameter = Expression.Parameter(typeof(object));
				var constructor = typeof(PropertyInfoAndValue).GetConstructor(new Type[] { typeof(PropertyInfo), typeof(object), typeof(string), typeof(string), typeof(bool), typeof(int) });

				var initializers = new List<Expression>();

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
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

					var newExpression = Expression.New(constructor, Expression.Constant(propertyInfo), Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, valueExpression, Expression.Constant(propertyInfo.PropertyType)), Expression.Constant(property.PropertyName), Expression.Constant(property.PersistedName), Expression.Constant(false), Expression.Constant(property.PropertyName.GetHashCode()));

					initializers.Add(newExpression);
				}

				var body = Expression.NewArrayInit(typeof(PropertyInfoAndValue), initializers);
				
				var lambdaExpression = Expression.Lambda(typeof(Func<object, PropertyInfoAndValue[]>), body, parameter);

				func = (Func<object, PropertyInfoAndValue[]>)lambdaExpression.Compile();

				propertyInfoAndValueGetterFuncByType[objectType] = func;
			}

			var propertyInfoAndValues = func(primaryKey); 
			
			return this.GetReferenceByPrimaryKey<T>(propertyInfoAndValues);
		}

		#region DataAccessObject factories

		public virtual IDataAccessObject NewDataAccessObject(Type type)
		{
			return NewDataAccessObject(type, false);
		}

		public virtual T NewDataAccessObject<T>()
			where T : IDataAccessObject
		{
			return NewDataAccessObject<T>(false);
		}

		public virtual IDataAccessObject NewDataAccessObject(Type type, bool transient)
		{
			var retval = this.assemblyBuildInfo.NewDataAccessObject(type);

			retval.SetIsNew(true);
			retval.SetDataAccessModel(this);

			if (!transient)
			{
				this.GetCurrentDataContext(false).CacheObject(retval, false);
			}
			else
			{
				retval.SetTransient(true);
			}

			return retval;
		}

		public virtual T NewDataAccessObject<T>(bool transient)
			where T : IDataAccessObject
		{
			var retval = this.assemblyBuildInfo.NewDataAccessObject<T>();

			retval.SetIsNew(true);
			retval.SetDataAccessModel(this);

			if (!transient)
			{
				this.GetCurrentDataContext(false).CacheObject(retval, false);
			}
			else
			{
				retval.SetTransient(true);
			}

			return retval;
		}

		#endregion

		public virtual DatabaseConnection GetCurrentDatabaseConnection()
		{
			return this.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite);
		}

		public virtual DatabaseConnection GetCurrentDatabaseConnection(DatabaseReadMode mode)
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(false);
			
			if (transactionContext.DatabaseConnection == null)
			{
				transactionContext.DatabaseConnection = this.databaseConnections[0];
			}
			
			return transactionContext.DatabaseConnection;
		}

		public virtual void Create()
		{
			this.Create(DatabaseCreationOptions.IfNotExist);
		}

		public virtual void Create(DatabaseCreationOptions options)
		{
			//this.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite).NewOldDatabaseCreator(this).CreateDatabase((options & DatabaseCreationOptions.DeleteExisting) != 0);
			this.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite).NewDatabaseCreator(this).Create((options & DatabaseCreationOptions.DeleteExisting) != 0);
		}

		/*
		public virtual MigrationPlan CreateMigrationPlan()
		{
			return CreateMigrationPlan(this.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite));
		}

		protected virtual MigrationPlan CreateMigrationPlan(DatabaseConnection connection)
		{
			var retval = new MigrationPlan();

			var planCreator = connection.NewMigrationPlanCreator(this);

			var plan = planCreator.CreateMigrationPlan();

			retval.AddDatabaseMigrationPlan(connection, plan);

			return retval;
		}

		public virtual MigrationScripts CreateMigrationScripts()
		{
			return CreateMigrationScripts(this.Configuration.PersistenceContexts.Select(x => new DataAccessModelDatabaseConnectionInfo(x.ContextName)).ToArray());
		}

		public virtual MigrationScripts CreateMigrationScripts(params DataAccessModelDatabaseConnectionInfo[] databaseConnectionInfos)
		{
			var retval = new MigrationScripts();

			foreach (var databaseConnectionInfo in databaseConnectionInfos)
			{
				var persistenceContext = this.GetDatabaseConnection(databaseConnectionInfo.ConnectionName);

				var applicator = persistenceContext.NewMigrationPlanApplicator(this);
				var plan = persistenceContext.NewMigrationPlanCreator(this).CreateMigrationPlan();

				retval.AddScripts(this, applicator.CreateScripts(plan));
			}

			return retval;
		}
		*/

		public virtual void FlushCurrentTransaction()
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(true);

			this.GetCurrentDataContext(true).Commit(transactionContext, true);
		}

		private volatile Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>> inflateFuncsByType = new Dictionary<Type, Func<IDataAccessObject, IDataAccessObject>>();

		public virtual IDataAccessObject Inflate(IDataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException("dataAccessObject");
			}

			var definitionType = dataAccessObject.DefinitionType;
			
			Func<IDataAccessObject, IDataAccessObject> func;

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
			var t = this.ToString();
			var s = this.GetDataAccessObjects<T>();

			return this.GetDataAccessObjects<T>().First(c => c == obj);
		}
	}
}
