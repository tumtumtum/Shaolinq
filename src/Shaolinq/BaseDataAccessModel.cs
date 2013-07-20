using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;
using Platform;

namespace Shaolinq
{
	public abstract class BaseDataAccessModel
		 : MarshalByRefObject, IDisposable
	{
		#region TranslateTo

		private static readonly MethodInfo BaseDataAccessModelTranslateToHelperMethod = typeof(BaseDataAccessModel).GetMethod("TranslateToHelper", BindingFlags.Static | BindingFlags.NonPublic);

		private static class ObjectToObjectProjectionProxyFunctionCache<U>
		{
			private static volatile Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>> translateToCache = new Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>>();

			public static Func<ProjectionContext, IDataAccessObject, U> GetProxyFunction(Type sourceType)
			{
				var key = sourceType;
				Func<ProjectionContext, IDataAccessObject, U> retval;

				if (!translateToCache.TryGetValue(key, out retval))
				{
					var translationContextParameter = Expression.Parameter(typeof(ProjectionContext), "projectionContext");
					var parameter = Expression.Parameter(typeof(IDataAccessObject), "dataAccessObject");
					var argument = Expression.Convert(parameter, sourceType);
					var body = Expression.Call(null, BaseDataAccessModelTranslateToHelperMethod.MakeGenericMethod(sourceType, typeof(U)), translationContextParameter, argument);
					var proxy = Expression.Lambda(body, translationContextParameter, parameter);

					retval = (Func<ProjectionContext, IDataAccessObject, U>)proxy.Compile();

					var newCache = new Dictionary<Type, Func<ProjectionContext, IDataAccessObject, U>>(translateToCache);

					newCache[key] = retval;

					translateToCache = newCache;
				}

				return retval;
			}
		}

		private static class ObjectToObjectProjectionFunctionCache<T, U>
		{
			private static volatile Func<ProjectionContext, T, U> CachedFunction;

			public static Func<ProjectionContext, T, U> GetCachedFunction()
			{
				Func<ProjectionContext, T, U> retval;

				if (CachedFunction == null)
				{
					retval = ObjectToObjectProjector<T, U>.Default.BuildProjectIntoNew();

					CachedFunction = retval;
				}
				else
				{
					retval = CachedFunction;
				}

				return retval;
			}
		}

		public U TranslateTo<U>(IDataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				return default(U);
			}

			var proxyFunction = ObjectToObjectProjectionProxyFunctionCache<U>.GetProxyFunction(dataAccessObject.GetType());

			return proxyFunction(this.DataAccessObjectProjectionContext, dataAccessObject);
		}

		internal static DESTINATION_TYPE TranslateToHelper<SOURCE_TYPE, DESTINATION_TYPE>(ProjectionContext projectionContext, SOURCE_TYPE dataAccessObject)
		{
			var function = ObjectToObjectProjectionFunctionCache<SOURCE_TYPE, DESTINATION_TYPE>.GetCachedFunction();

			return function(projectionContext, dataAccessObject);
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

		#endregion

		public Assembly DefinitionAssembly { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }

		#region Disposer and Finalizer

		public event EventHandler Disposed;

		protected virtual void OnDisposed(EventArgs eventArgs)
		{
			if (this.Disposed != null)
			{
				this.Disposed(this, eventArgs);
			}
		}

		~BaseDataAccessModel()
		{
			Dispose();
		}

		private volatile int disposed = 0;

		/// <summary>
		/// Removes all cached connections for the database model associated with the current thread.
		/// </summary>
		public virtual void FlushConnections()
		{
			DataAccessModelTransactionManager.GetAmbientTransactionManager(this).FlushConnections();
			
			foreach (var persistenceContext in this.persistenceContextsByType.Values)
			{
				persistenceContext.DropAllConnections();
			}
		}

		public virtual void Dispose()
		{
#pragma warning disable 420
			FlushConnections();

			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				FlushConnections();

				this.OnDisposed(EventArgs.Empty);

				GC.SuppressFinalize(this);
			}
#pragma warning restore 420
		}

		#endregion

		#region GetTypeDescriptor

		public virtual ModelTypeDescriptor ModelTypeDescriptor
		{
			get;
			private set;
		}

		public virtual TypeDescriptor GetTypeDescriptor(Type type)
		{
			return TypeDescriptorProvider.GetProvider(type.Assembly).GetTypeDescriptor(this.GetDefinitionTypeFromConcreteType(type));
		}

		#endregion

		#region TryGetPersistenceContext

		private readonly Dictionary<Type, PersistenceContext> persistenceContextsByType = new Dictionary<Type, PersistenceContext>();

		public PersistenceContext GetPersistenceContext(IDataAccessObject dataAccessObject)
		{
			return GetPersistenceContext(dataAccessObject.GetType());
		}

		public PersistenceContext GetPersistenceContext(Type type)
		{
			PersistenceContext persistenceContext;

			var concreteType = this.GetConcreteTypeFromDefinitionType(type);

			if (!persistenceContextsByType.TryGetValue(concreteType, out persistenceContext))
			{
				var buffer = new StringBuilder();

				buffer.AppendFormat("Unable to find persistence context for type: {0}.  Available contexts: ", concreteType);

				var x = 0;

				foreach (var context in persistenceContextsByType.Keys)
				{
					if (x++ != 0)
					{
						buffer.Append(", ");
					}

					buffer.Append(context.Name);
				}

				throw new InvalidOperationException(buffer.ToString());
			}

			return persistenceContext;
		}

		#endregion

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

		#region DataAccessModelStorage

		private readonly Dictionary<object, object> dataAccessModelStorage = new Dictionary<object, object>();

		internal virtual T GetDataAccessModelStorage<T>(object identifier)
			where T : class
		{
			lock (dataAccessModelStorage)
			{
				object retval;

				if (dataAccessModelStorage.TryGetValue(identifier, out retval))
				{
					return (T)retval;
				}
			}

			return null;
		}

		internal virtual void SetDataAccessModelStorage<T>(object identifier, T value)
			where T : class
		{
			lock (this)
			{
				dataAccessModelStorage[identifier] = value;
			}
		}

		#endregion DataAccessModelStoragew

		#region BuildDataAccessModel

		public AssemblyBuildInfo AssemblyBuildInfo
		{
			get
			{
				return assemblyBuildInfo;
			}
		}

		private AssemblyBuildInfo assemblyBuildInfo;

		private void SetAssemblyBuildInfo(AssemblyBuildInfo value)
		{
			this.assemblyBuildInfo = value;

			this.DefinitionAssembly = value.definitionAssembly;
			this.TypeDescriptorProvider = TypeDescriptorProvider.GetProvider(this.DefinitionAssembly);
			this.ModelTypeDescriptor = this.TypeDescriptorProvider.GetModelTypeDescriptor(value.GetDefinitionModelType(this.GetType()));

			foreach (var keyValuePair in value.GetQueryablePersistenceContextNamesByModelType(this.GetType()))
			{
				PersistenceContext persistenceContext;
				if (this.TryGetPersistenceContext(keyValuePair.Value, out persistenceContext))
				{
					this.persistenceContextsByType[keyValuePair.Key] = persistenceContext;
				}
			}
		}

		public static T BuildDataAccessModel<T>()
			where T : BaseDataAccessModel
		{
			return BuildDataAccessModel<T>(null);
		}

		public static T BuildDataAccessModel<T>(DataAccessModelConfiguration configuration)
			where T : BaseDataAccessModel
		{
			var builder = DataAccessModelAssemblyBuilder.Default;
			var buildInfo = builder.GetOrBuildConcreteAssembly(typeof(T).Assembly);

			var retval = buildInfo.NewDataAccessModel<T>();

			retval.SetConfiguration(configuration);
			retval.SetAssemblyBuildInfo(buildInfo);

			retval.Initialise();

			return retval;
		}

		public DataAccessModelConfiguration GetDefaultConfiguration()
		{
			var configuration = ConfigurationBlock<DataAccessModelConfiguration>.Load(this.GetType().Namespace.SplitAroundCharFromRight('.').Right);

			if (configuration == null)
			{
				configuration = ConfigurationBlock<DataAccessModelConfiguration>.Load(this.GetType().Namespace.SplitAroundCharFromRight('.').Right + "/DataAccessModel");
			}

			if (configuration == null)
			{
				configuration = ConfigurationBlock<DataAccessModelConfiguration>.Load(this.GetType().Namespace.SplitAroundCharFromRight('.').Right + "/Configuration");
			}

			if (configuration == null)
			{
				configuration = ConfigurationBlock<DataAccessModelConfiguration>.Load(this.GetType().Namespace.Replace('.', '/') + "/Configuration");
			}

			return configuration;
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

		[ReflectionEmitted]
		protected abstract void Initialise();

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

		public virtual T ReferenceToDataAccessObject<T>(object primaryKey)
			where T : IDataAccessObject
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
						valueExpression = Expression.PropertyOrField(Expression.Convert(parameter, objectType), property.PropertyName);
					}

					var newExpression = Expression.New(constructor, Expression.Constant(property.PropertyInfo), Expression.Convert(valueExpression, typeof(object)), Expression.Constant(property.PropertyName), Expression.Constant(property.PersistedName), Expression.Constant(false), Expression.Constant(property.PropertyName.GetHashCode()));

					initializers.Add(newExpression);
				}

				var body = Expression.NewArrayInit(typeof(PropertyInfoAndValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object, PropertyInfoAndValue[]>), body, parameter);

				func = (Func<object, PropertyInfoAndValue[]>)lambdaExpression.Compile();

				propertyInfoAndValueGetterFuncByType[objectType] = func;
			}

			var propertyInfoAndValues = func(primaryKey);
			var existing = this.GetCurrentDataContext(false).GetObject(this.GetPersistenceContext(typeof(T)), this.GetConcreteTypeFromDefinitionType(typeof(T)), propertyInfoAndValues);

			if (existing != null)
			{
				return (T)existing;
			}
			else
			{
				var retval = this.assemblyBuildInfo.NewDataAccessObject<T>();

				retval.SetIsNew(false);
				retval.SetIsWriteOnly(true);
				retval.SetDataAccessModel(this);

				return retval;
			}
		}

		#region NewDataAccessObject

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

			if (retval.NumberOfPrimaryKeys == 1 && retval.DefinesAutoIncrementKey && (retval.KeyType == typeof(Guid) || retval.KeyType == typeof(Guid?)))
			{
				retval.SetAutoIncrementKeyValue(Guid.NewGuid());
			}

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

			if (retval.NumberOfPrimaryKeys == 1 && retval.DefinesAutoIncrementKey && (retval.KeyType == typeof(Guid) || retval.KeyType == typeof(Guid?)))
			{
				retval.SetAutoIncrementKeyValue(Guid.NewGuid());
			}

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

		#region TryGetPersistenceContext

		internal protected virtual PersistenceContext GetPersistenceContext(string persistenceContextName)
		{
			PersistenceContext retval;
			PersistenceContextProvider persistenceContextProvider;

			if (!Configuration.TryGetDatabaseContextProvider(persistenceContextName, out persistenceContextProvider)
				|| !persistenceContextProvider.TryGetPersistenceContext(PersistenceMode.ReadWrite, out retval))
			{
				throw new InvalidOperationException("Unable to find persistence context: " + persistenceContextName);
			}

			return retval;
		}

		internal protected virtual bool TryGetPersistenceContext(string persistenceContextName, out PersistenceContext persistenceContext)
		{
			PersistenceContextProvider persistenceContextProvider;

			if (!Configuration.TryGetDatabaseContextProvider(persistenceContextName, out persistenceContextProvider))
			{
				persistenceContext = null;
				return false;
			}

			return persistenceContextProvider.TryGetPersistenceContext(PersistenceMode.ReadWrite, out persistenceContext);
		}

		#endregion

		#region CreateDatabases

		/// <summary>
		/// Creates the a database from the model for all persistence contexts
		/// </summary>
		public virtual int CreateDatabases(bool overwrite)
		{
			if (overwrite)
			{
				FlushConnections();
			}
			
			return CreateDatabases(overwrite, this.Configuration.PersistenceContexts.Select(x => new DataAccessModelPersistenceContextInfo(x.ContextName)).ToArray());
		}

		/// <summary>
		/// Creates the a database from the data access model
		/// </summary>
		/// <param name="persistenceContextInfos">An array of persistence contexts that define the databases to use</param>
		public virtual int CreateDatabases(bool overwrite, params DataAccessModelPersistenceContextInfo[] persistenceContextInfos)
		{
			int retval = 0;

			foreach (var persistenceContextInfo in persistenceContextInfos)
			{
				var persistenceContext = this.GetPersistenceContext(persistenceContextInfo.ContextName);

				var databaseCreator = persistenceContext.NewPersistenceStoreCreator(this, persistenceContextInfo);

				if (databaseCreator.CreatePersistenceStorage(overwrite))
				{
					retval++;
				}
			}

			return retval;
		}

		public virtual MigrationPlan CreateMigrationPlan()
		{
			return CreateMigrationPlan(this.Configuration.PersistenceContexts.Select(x => new DataAccessModelPersistenceContextInfo(x.ContextName)).ToArray());
		}

		protected virtual MigrationPlan CreateMigrationPlan(params DataAccessModelPersistenceContextInfo[] persistenceContextInfos)
		{
			var retval = new MigrationPlan();

			foreach (var persistenceContextInfo in persistenceContextInfos)
			{
				var persistenceContext = this.GetPersistenceContext(persistenceContextInfo.ContextName);

				var planCreator = persistenceContext.NewMigrationPlanCreator(this, persistenceContextInfo);

				var plan = planCreator.CreateMigrationPlan();

				retval.AddPersistenceContextMigrationPlan(persistenceContextInfo, plan);
			}

			return retval;
		}

		public virtual MigrationScripts CreateMigrationScripts()
		{
			return CreateMigrationScripts(this.Configuration.PersistenceContexts.Select(x => new DataAccessModelPersistenceContextInfo(x.ContextName)).ToArray());
		}

		public virtual MigrationScripts CreateMigrationScripts(params DataAccessModelPersistenceContextInfo[] persistenceContextInfos)
		{
			var retval = new MigrationScripts();

			foreach (var persistenceContextInfo in persistenceContextInfos)
			{
				var persistenceContext = this.GetPersistenceContext(persistenceContextInfo.ContextName);

				var applicator = persistenceContext.NewMigrationPlanApplicator(this, persistenceContextInfo);
				var plan = persistenceContext.NewMigrationPlanCreator(this, persistenceContextInfo).CreateMigrationPlan();

				retval.AddScripts(this, applicator.CreateScripts(plan));
			}

			return retval;
		}

		#endregion

		public virtual void FlushCurrentTransaction()
		{
			var transactionContext = this.AmbientTransactionManager.GetCurrentContext(true);

			this.GetCurrentDataContext(true).Commit(transactionContext, true);
		}
	}
}
