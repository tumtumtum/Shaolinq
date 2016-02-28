// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public partial class DataAccessModel
		 : IDisposable
	{
		#region ContextData

		internal readonly AsyncLocal<ContextData> asyncState = new AsyncLocal<ContextData>();

		internal struct ContextData
		{
			public int version;
			public TransactionContext transactionContext;

			public ContextData(int version, TransactionContext transactionContext)
			{
				this.version = version;
				this.transactionContext = transactionContext;
			}
		}

		internal int AsyncLocalExecutionVersion
		{
			get { return asyncState.Value.version; }
			set { asyncState.Value = new ContextData(value, asyncState.Value.transactionContext); }
		}

		internal TransactionContext AsyncLocalTransactionContext
		{
			get { return asyncState.Value.transactionContext; }
			set { asyncState.Value = new ContextData(asyncState.Value.version, value); }
		}

		#endregion

		#region Nested Types
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
		#endregion

		public virtual event EventHandler Disposed;

		private bool disposed;
		public Assembly DefinitionAssembly { get; private set; }
		public ModelTypeDescriptor ModelTypeDescriptor { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public RuntimeDataAccessModelInfo RuntimeDataAccessModelInfo { get; private set; }
		private Dictionary<RuntimeTypeHandle, Func<IQueryable>> createDataAccessObjectsFuncs = new Dictionary<RuntimeTypeHandle, Func<IQueryable>>();
		private readonly Dictionary<string, SqlDatabaseContextsInfo> sqlDatabaseContextsByCategory = new Dictionary<string, SqlDatabaseContextsInfo>(StringComparer.InvariantCultureIgnoreCase);
		private Dictionary<RuntimeTypeHandle, Func<DataAccessObject, DataAccessObject>> inflateFuncsByType = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, DataAccessObject>>();
		private Dictionary<RuntimeTypeHandle, Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>> inflateAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>>();
		private Dictionary<RuntimeTypeHandle, Func<object, ObjectPropertyValue[]>> propertyInfoAndValueGetterFuncByType = new Dictionary<RuntimeTypeHandle, Func<object, ObjectPropertyValue[]>>();

		internal Dictionary<TypeRelationshipInfo, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>> relatedDataAccessObjectsInitializeActionsCache = new Dictionary<TypeRelationshipInfo, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(TypeRelationshipInfoEqualityComparer.Default);
		
		public virtual DataAccessObjects<T> GetDataAccessObjects<T>()
			where T : DataAccessObject
		{
			return (DataAccessObjects<T>) this.GetDataAccessObjects(typeof(T));
		}

		internal virtual IQueryable CreateDataAccessObjects(RuntimeTypeHandle typeHandle)
		{
			Func<IQueryable> func;

			if (!this.createDataAccessObjectsFuncs.TryGetValue(typeHandle, out func))
			{
				var type = Type.GetTypeFromHandle(typeHandle);

				var constructor = typeof(DataAccessObjects<>).MakeGenericType(type).GetConstructor(new [] { typeof(DataAccessModel), typeof(Expression) });

				Debug.Assert(constructor != null);

				var body = Expression.Convert(Expression.New(constructor, Expression.Constant(this), Expression.Constant(null, typeof(Expression))), typeof(IQueryable));

				func = Expression.Lambda<Func<IQueryable>>(body).Compile();

				var newDictionary = new Dictionary<RuntimeTypeHandle, Func<IQueryable>>(this.createDataAccessObjectsFuncs) { [typeHandle] = func };


				this.createDataAccessObjectsFuncs = newDictionary;
			}

			return func();
		}

		internal virtual IQueryable CreateDataAccessObjects(Type type)
		{
			return this.CreateDataAccessObjects(type.TypeHandle);
		}

		public TransactionContext GetCurrentContext(bool forWrite)
		{
			return TransactionContext.GetCurrentContext(this, forWrite);
		}
		
		protected virtual void OnDisposed(EventArgs eventArgs)
		{
			this.Disposed?.Invoke(this, eventArgs);
		}

		internal bool IsDisposed => this.disposed;

		~DataAccessModel()
		{
			this.Dispose(false);
		}

		private void DisposeAllSqlDatabaseContexts()
		{
			foreach (var context in this.sqlDatabaseContextsByCategory.Values.SelectMany(c => c.DatabaseContexts))
			{
				context.Dispose();
			}
		}

		public void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}

			ActionUtils.IgnoreExceptions(() => this.AsyncLocalTransactionContext?.Dispose());
			ActionUtils.IgnoreExceptions(() => this.asyncState.Dispose());

			this.asyncState.Value = default(ContextData);
			this.disposed = true;
			this.DisposeAllSqlDatabaseContexts();
			this.OnDisposed(EventArgs.Empty);
		}

		internal Type GetConcreteTypeFromDefinitionType(Type definitionType)
		{
			return this.RuntimeDataAccessModelInfo.GetConcreteType(definitionType);
		}

		internal Type GetDefinitionTypeFromConcreteType(Type concreteType)
		{
			return this.RuntimeDataAccessModelInfo.GetDefinitionType(concreteType);
		}

		public virtual TypeDescriptor GetTypeDescriptor(Type type)
		{
			return this.TypeDescriptorProvider.GetTypeDescriptor(this.GetDefinitionTypeFromConcreteType(type));
		}

		public DataAccessObjectDataContext GetCurrentDataContext(bool forWrite)
		{
			return TransactionContext.GetCurrentContext(this, forWrite).GetCurrentDataContext();
		}

		public SqlTransactionalCommandsContext GetCurrentSqlDatabaseTransactionContext()
		{
			return TransactionContext.GetCurrentContext(this, true).GetCurrentTransactionalCommandsContext(this.GetCurrentSqlDatabaseContext());
		}

		private void SetAssemblyBuildInfo(RuntimeDataAccessModelInfo value)
		{
			this.RuntimeDataAccessModelInfo = value;
			this.DefinitionAssembly = value.DefinitionAssembly;
			this.TypeDescriptorProvider = value.TypeDescriptorProvider;
			this.ModelTypeDescriptor = this.TypeDescriptorProvider.ModelTypeDescriptor;

			foreach (var contextInfo in this.Configuration.SqlDatabaseContextInfos)
			{
				SqlDatabaseContextsInfo info;
				var newSqlDatabaseContext = contextInfo.CreateSqlDatabaseContext(this);

				if (newSqlDatabaseContext.ContextCategories.Length == 0)
				{
					if (!this.sqlDatabaseContextsByCategory.TryGetValue("*", out info))
					{
						info = SqlDatabaseContextsInfo.Create();

						this.sqlDatabaseContextsByCategory["*"] = info;
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

			if (!this.sqlDatabaseContextsByCategory.ContainsKey("*"))
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
			if (!dataAccessModelType.IsSubclassOf(typeof(DataAccessModel)))
			{
				throw new ArgumentException("Data access model type must derive from DataAccessModel", nameof(dataAccessModelType));
			}

			configuration = configuration ?? GetDefaultConfiguration(dataAccessModelType);

			if (configuration == null)
			{
				throw new InvalidOperationException("No configuration specified or declaredd");
			}

			var buildInfo = CachingDataAccessModelAssemblyProvider.Default.GetDataAccessModelAssembly(dataAccessModelType, configuration);
			var retval = buildInfo.NewDataAccessModel();

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
			return (T)BuildDataAccessModel(typeof(T), configuration);
		}

		public static DataAccessModelConfiguration GetConfiguration(string path)
		{
			return (DataAccessModelConfiguration)ConfigurationManager.GetSection(path);
		}

		public static DataAccessModelConfiguration GetDefaultConfiguration(Type type)
		{
			var typeName = type.Name;
			var configuration = GetConfiguration(typeName);

			if (configuration != null)
			{
				return configuration;
			}

			if (typeName.EndsWith("DataAccessModel"))
			{
				configuration = GetConfiguration(typeName.Left(typeName.Length - "DataAccessModel".Length));

				return configuration;
			}

			return null;
		}

		internal void SetConfiguration(DataAccessModelConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		protected internal virtual T GetReference<T>(ObjectPropertyValue[] primaryKey)
			where T : DataAccessObject
		{
			return (T) this.GetReference(typeof(T), primaryKey);
		}

		protected internal virtual DataAccessObject GetReference<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			var primaryKeyValues = this.GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			return this.GetReference(type, primaryKeyValues);
		}

		protected internal virtual DataAccessObject GetReference(Type type, ObjectPropertyValue[] primaryKey)
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
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, false);

				var internalDataAccessObject = retval.ToObjectInternal();

				internalDataAccessObject.SetIsDeflatedReference(true);
				internalDataAccessObject.SetPrimaryKeys(objectPropertyAndValues);
				internalDataAccessObject.ResetModified();
				internalDataAccessObject.FinishedInitializing();
				internalDataAccessObject.SubmitToCache();

				return retval;
			}
		}

		protected internal virtual T GetReference<T>(object[] primaryKeyValues)
			where T : DataAccessObject
		{
			var propertyValues = this.GetObjectPropertyValues<T>(primaryKeyValues);

			return this.GetReference<T>(propertyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValues<T>(object[] primaryKeyValues)
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException(nameof(primaryKeyValues));
			}

			if (primaryKeyValues.All(c => c == null))
			{
				return null;
			}

			Func<object, ObjectPropertyValue[]> func;
			var objectTypeHandle = typeof(RawPrimaryKeysPlaceholderType<T>).TypeHandle;

			if (!this.propertyInfoAndValueGetterFuncByType.TryGetValue(objectTypeHandle, out func))
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
						convertedValue = Expression.Convert(valueExpression, typeof(object));
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

				var newPropertyInfoAndValueGetterFuncByType = new Dictionary<RuntimeTypeHandle, Func<object, ObjectPropertyValue[]>>(this.propertyInfoAndValueGetterFuncByType)
				{
					[objectTypeHandle] = func
				};

				this.propertyInfoAndValueGetterFuncByType = newPropertyInfoAndValueGetterFuncByType;
			}

			return func(primaryKeyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValues<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (Equals(primaryKey, default(K)) && typeof(K).IsClass)
			{
				throw new ArgumentNullException(nameof(primaryKey));
			}

			var idType = primaryKey.GetType();
			var primaryKeyTypeHandle = Type.GetTypeHandle(primaryKey);
			Func<object, ObjectPropertyValue[]> func;

			if (!this.propertyInfoAndValueGetterFuncByType.TryGetValue(primaryKeyTypeHandle, out func))
			{
				var isSimpleKey = false;
				var typeOfPrimaryKey = Type.GetTypeFromHandle(primaryKeyTypeHandle);
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(type);
				var idPropertyType = typeDescriptor.PrimaryKeyProperties[0].PropertyType;

				if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(idType) || (idPropertyType.IsAssignableFrom(idType) && primaryKeyType == PrimaryKeyType.Auto))
				{
					isSimpleKey = true;
				}

				if (isSimpleKey && typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new InvalidOperationException("Composite primary key expected");
				}

				var parameter = Expression.Parameter(typeof(object));
				var constructor = ConstructorInfoFastRef.ObjectPropertyValueConstructor;
				var typedParameter = Expression.Convert(parameter, typeOfPrimaryKey);

				var initializers = new List<Expression>();
				var replacementPrimaryKeyValues = new Dictionary<string, Expression>();

				if (typeDescriptor.PrimaryKeyDerivableProperties.Count > 0)
				{
					var properties = typeDescriptor
						.PrimaryKeyDerivableProperties
						.Where(c => idType.GetProperty(c.PropertyName, BindingFlags.Instance | BindingFlags.Public) != null)
						.ToList();

					replacementPrimaryKeyValues = properties.ToDictionary
					(
						c => c.ComputedMemberAssignTarget.Name, 
						c => MemberAccessReplacer.Replace
						(
							c.ComputedMemberAssignmentValue, c.PropertyInfo,
							Expression.Property(typedParameter, c.PropertyName)
						)
					);
				}

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
						if (!replacementPrimaryKeyValues.TryGetValue(property.PropertyName, out valueExpression))
						{
							valueExpression = Expression.PropertyOrField(typedParameter, property.PropertyName);
						}
					}

					Expression primaryKeyValue;

					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(this.GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

					if (isObjectType)
					{
						var method = MethodInfoFastRef.DataAccessModelGetReferenceMethod.MakeGenericMethod(propertyInfo.PropertyType, valueExpression.Type);

						if (isSimpleKey || valueExpression.Type.IsDataAccessObjectType())
						{
							primaryKeyValue = valueExpression;
						}
						else
						{
							primaryKeyValue = Expression.Call(Expression.Constant(this), method, valueExpression, Expression.Constant(PrimaryKeyType.Composite));
						}
					}
					else
					{
						primaryKeyValue = Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, Expression.Convert(valueExpression, typeof(object)), Expression.Constant(propertyInfo.PropertyType));
					}

					var newExpression = Expression.New
					(
						constructor,
						Expression.Constant(propertyInfo.PropertyType),
						Expression.Constant(property.PropertyName),
						Expression.Constant(property.PersistedName),
						Expression.Constant(property.PropertyName.GetHashCode()),
						Expression.Convert(primaryKeyValue, typeof(object))
					);

					initializers.Add(newExpression);
				}

				var body = Expression.NewArrayInit(typeof(ObjectPropertyValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object, ObjectPropertyValue[]>), body, parameter);

				func = (Func<object, ObjectPropertyValue[]>)lambdaExpression.Compile();

				var newPropertyInfoAndValueGetterFuncByType = new Dictionary<RuntimeTypeHandle, Func<object, ObjectPropertyValue[]>>(this.propertyInfoAndValueGetterFuncByType) { [primaryKeyTypeHandle] = func };

				this.propertyInfoAndValueGetterFuncByType = newPropertyInfoAndValueGetterFuncByType;
			}

			return func(primaryKey);
		}

		public virtual T GetReference<T, K>(K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
			where T : DataAccessObject
		{
			var propertyValues = this.GetObjectPropertyValues<K>(typeof(T), primaryKey, primaryKeyType);

			return this.GetReference<T>(propertyValues);
		}

		public virtual T GetReference<T, K>(Expression<Func<K, T>> condition, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
			where T : DataAccessObject
		{
			return null;
		}

		public virtual DataAccessObject CreateDataAccessObject(Type type)
		{
			var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, true);
			var retvalInternal = retval.ToObjectInternal();

			retvalInternal.FinishedInitializing();
			retvalInternal.SubmitToCache();

			return retval;
		}

		public virtual IDataAccessObjectAdvanced CreateDataAccessObject<K>(Type type, K primaryKey)
		{
			return this.CreateDataAccessObject(type, primaryKey, PrimaryKeyType.Auto);
		}

		public virtual IDataAccessObjectAdvanced CreateDataAccessObject<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			if (!typeof(IDataAccessObjectAdvanced).IsAssignableFrom(type)
				|| !typeof(DataAccessObject<>).IsAssignableFromIgnoreGenericParameters(type))
			{
				throw new ArgumentException("Type must be a DataAccessObjectType", nameof(type));
			}

			var objectPropertyAndValues = this.GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			if (objectPropertyAndValues.Any(keyValue => keyValue.Value == null))
			{
				throw new MissingOrInvalidPrimaryKeyException();
			}

			var existing = this.GetCurrentDataContext(false).GetObject(this.GetConcreteTypeFromDefinitionType(type), objectPropertyAndValues);

			if (existing != null)
			{
				IDataAccessObjectAdvanced obj = null;

				ActionUtils.IgnoreExceptions(() => obj = this.GetReference(type, primaryKey, primaryKeyType));

				throw new ObjectAlreadyExistsException(obj, null, "CreateDataAccessObject");
			}
			else
			{
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, true);

				retval.ToObjectInternal().SetPrimaryKeys(objectPropertyAndValues);
				retval.ToObjectInternal().FinishedInitializing();
				retval.ToObjectInternal().SubmitToCache();

				return retval;
			}
		}

		public virtual T CreateDataAccessObject<T>()
			where T : DataAccessObject
		{
			var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject<T>(this, true);

			retval.ToObjectInternal().FinishedInitializing();
			retval.ToObjectInternal().SubmitToCache();

			return retval;
		}

		public virtual T CreateDataAccessObject<T, K>(K primaryKey)
			where T : DataAccessObject
		{
			return this.CreateDataAccessObject<T, K>(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T CreateDataAccessObject<T, K>(K primaryKey, PrimaryKeyType primaryKeyType)
			where T : DataAccessObject
		{
			var objectPropertyAndValues = this.GetObjectPropertyValues<K>(typeof(T), primaryKey, primaryKeyType);

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
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject<T>(this, true);

				retval.ToObjectInternal().SetPrimaryKeys(objectPropertyAndValues);
				retval.ToObjectInternal().FinishedInitializing();
				retval.ToObjectInternal().SubmitToCache();

				return retval;
			}
		}
		
		public virtual SqlDatabaseContext GetCurrentSqlDatabaseContext()
		{
			var forWrite = DataAccessTransaction.Current != null;

			var transactionContext = this.GetCurrentContext(forWrite);

			if (transactionContext.sqlDatabaseContext != null)
			{
				return transactionContext.sqlDatabaseContext;
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

			transactionContext.sqlDatabaseContext = info.DatabaseContexts[index];

			return transactionContext.sqlDatabaseContext;
		}

		public virtual void SetCurrentTransactionDatabaseCategories(params string[] categories)
		{
			var transactionContext = this.GetCurrentContext(false);
			
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
			this.Create(DatabaseCreationOptions.IfDatabaseNotExist);
		}

		public virtual void Create(DatabaseCreationOptions options)
		{
			using (var scope = new DataAccessScope(DataAccessIsolationLevel.Unspecified, DataAccessScopeOptions.RequiresNew, TimeSpan.FromMinutes(10)))
			{
				this.GetCurrentSqlDatabaseContext().SchemaManager.CreateDatabaseAndSchema(options);

				scope.Complete();
			}
		}

		[RewriteAsync]
		public virtual void Flush()
		{
			var transactionContext = this.GetCurrentContext(true);

			using (var context = transactionContext.AcquireVersionContext())
			{
				this.GetCurrentDataContext(true).Commit(transactionContext, true);
			}
		}

		protected internal ISqlQueryProvider NewQueryProvider()
		{
			return this.GetCurrentSqlDatabaseContext().CreateQueryProvider();
		}

		protected internal DataAccessObject Inflate(DataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException(nameof(dataAccessObject));
			}

			Func<DataAccessObject, DataAccessObject> func;
			var definitionTypeHandle = dataAccessObject.GetAdvanced().DefinitionType.TypeHandle;
			
			if (!this.inflateFuncsByType.TryGetValue(definitionTypeHandle, out func))
			{
				var definitionType = Type.GetTypeFromHandle(definitionTypeHandle);
				var parameter = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "dataAccessObject");
				var methodInfo = MethodInfoFastRef.DataAccessModelGenericInflateHelperMethod.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType));

				var lambda = Expression.Lambda<Func<DataAccessObject, DataAccessObject>>(body, parameter);

				func = lambda.Compile();

				var newDictionary = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, DataAccessObject>>(this.inflateFuncsByType)
				{
					[definitionTypeHandle] = func
				};

				this.inflateFuncsByType = newDictionary;
			}

			return func(dataAccessObject);
		}

		protected internal Task<DataAccessObject> InflateAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException(nameof(dataAccessObject));
			}

			Func<DataAccessObject, CancellationToken, Task<DataAccessObject>> func;
			var definitionTypeHandle = dataAccessObject.GetAdvanced().DefinitionType.TypeHandle;

			if (!this.inflateAsyncFuncsByType.TryGetValue(definitionTypeHandle, out func))
			{
				var definitionType = Type.GetTypeFromHandle(definitionTypeHandle);
				var parameter = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "dataAccessObject");
				var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
				var methodInfo = MethodInfoFastRef.DataAccessModelGenericInflateAsyncHelperMethod.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType), cancellationTokenParameter);

				var lambda = Expression.Lambda<Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>>(body, parameter, cancellationTokenParameter);

				func = lambda.Compile();

				var newDictionary = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>>(this.inflateAsyncFuncsByType)
				{
					[definitionTypeHandle] = func
				};

				this.inflateAsyncFuncsByType = newDictionary;
			}

			return func(dataAccessObject, cancellationToken);
		}

		internal T InflateHelper<T>(T obj)
			where T : DataAccessObject
		{
			if (!obj.IsDeflatedReference())
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

		internal async Task<DataAccessObject> InflateAsyncHelper<T>(T obj, CancellationToken cancellationToken)
			where T : DataAccessObject
		{
			if (!obj.IsDeflatedReference())
			{
				return obj;
			}

			var retval = await this.GetDataAccessObjects<T>().FirstOrDefaultAsync(c => c == obj, cancellationToken);

			if (retval == null)
			{
				throw new MissingDataAccessObjectException(obj);
			}

			return retval;
		}

		public virtual DataAccessObjects<T> ExecuteProcedure<T>(string procedureName, object[] args)
			where T : DataAccessObject
		{
			return null;
		}

		protected virtual void Initialise()
		{
			throw new NotImplementedException();
		}

		public virtual IQueryable GetDataAccessObjects(Type type)
		{
			throw new NotImplementedException();
		}
	}
}
