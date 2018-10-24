// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Analytics;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

// ReSharper disable InconsistentlySynchronizedField

namespace Shaolinq
{
	public partial class DataAccessModel
		 : IDisposable, IDataAccessModelInternal
	{
		#region ContextData
		
		internal readonly AsyncLocal<int> asyncLocalExecutionVersion = new AsyncLocal<int>();
		internal readonly AsyncLocal<TransactionContext> asyncLocalTransactionalAmbientTransactionContext = new AsyncLocal<TransactionContext>();
		
		internal int AsyncLocalExecutionVersion
		{
			get { return this.asyncLocalExecutionVersion.Value; }
			set { this.asyncLocalExecutionVersion.Value = value; }
		}

		internal TransactionContext AsyncLocalAmbientTransactionContext
		{
			get { return this.asyncLocalTransactionalAmbientTransactionContext.Value; }
			set { this.asyncLocalTransactionalAmbientTransactionContext.Value = value; }
		}

		#endregion

		#region Nested Types
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

		internal QueryAnalytics queryAnalytics = new QueryAnalytics();

		public virtual IQueryAnalytics QueryAnalytics => this.queryAnalytics;

		public virtual event EventHandler Disposed;

		internal bool hasAnyAutoIncrementValidators;
		public Assembly DefinitionAssembly { get; private set; }
		public ModelTypeDescriptor ModelTypeDescriptor { get; private set; }
		public DataAccessModelConfiguration Configuration { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public RuntimeDataAccessModelInfo RuntimeDataAccessModelInfo { get; private set; }
		private Dictionary<RuntimeTypeHandle, Func<IQueryable>> createDataAccessObjectsFuncs = new Dictionary<RuntimeTypeHandle, Func<IQueryable>>();
		private readonly Dictionary<string, SqlDatabaseContextsInfo> sqlDatabaseContextsByCategory = new Dictionary<string, SqlDatabaseContextsInfo>(StringComparer.InvariantCultureIgnoreCase);
		private Dictionary<RuntimeTypeHandle, Func<DataAccessObject, DataAccessObject>> inflateFuncsByType = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, DataAccessObject>>();
		private Dictionary<RuntimeTypeHandle, Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>> inflateAsyncFuncsByType = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>>();
		private Dictionary<Pair<RuntimeTypeHandle, RuntimeTypeHandle>, Func<object, ObjectPropertyValue[]>> objectPropertyValuesByAnonymousKeyFuncByType = new Dictionary<Pair<RuntimeTypeHandle, RuntimeTypeHandle>, Func<object, ObjectPropertyValue[]>>();
		private Dictionary<RuntimeTypeHandle, Func<object[], ObjectPropertyValue[]>> objectPropertyValuesByColumnValuesFuncByType = new Dictionary<RuntimeTypeHandle, Func<object[], ObjectPropertyValue[]>>();
		private Dictionary<RuntimeTypeHandle, Func<object[], ObjectPropertyValue[]>> objectPropertyValuesByPrimaryKeyValuesFuncByType = new Dictionary<RuntimeTypeHandle, Func<object[], ObjectPropertyValue[]>>();
		

		internal readonly object hooksLock = new object();
		
		internal Dictionary<TypeRelationshipInfo, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>> relatedDataAccessObjectsInitializeActionsCache = new Dictionary<TypeRelationshipInfo, Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced>>(TypeRelationshipInfoEqualityComparer.Default);
		
		public virtual DataAccessObjects<T> GetDataAccessObjects<T>()
			where T : DataAccessObject
		{
			return (DataAccessObjects<T>) GetDataAccessObjects(typeof(T));
		}

		IQueryable IDataAccessModelInternal.CreateDataAccessObjects(RuntimeTypeHandle typeHandle)
		{
			if (!this.createDataAccessObjectsFuncs.TryGetValue(typeHandle, out var func))
			{
				var type = Type.GetTypeFromHandle(typeHandle);

				var constructor = typeof(DataAccessObjects<>).MakeGenericType(type).GetConstructor(new[] { typeof(DataAccessModel), typeof(Expression) });

				Debug.Assert(constructor != null);

				var body = Expression.Convert(Expression.New(constructor, Expression.Constant(this), Expression.Constant(null, typeof(Expression))), typeof(IQueryable));

				func = Expression.Lambda<Func<IQueryable>>(body).Compile();

				this.createDataAccessObjectsFuncs = this.createDataAccessObjectsFuncs.Clone(typeHandle, func);
			}

			return func();
		}

		IQueryable IDataAccessModelInternal.CreateDataAccessObjects(Type type)
		{
			return ((IDataAccessModelInternal)this).CreateDataAccessObjects(type.TypeHandle);
		}

		private TransactionContext GetCurrentContext(bool forWrite)
		{
			return TransactionContext.GetCurrent(this, forWrite);
		}
		
		protected virtual void OnDisposed(EventArgs eventArgs)
		{
			this.Disposed?.Invoke(this, eventArgs);
		}

		internal bool IsDisposed { get; private set; }

		~DataAccessModel()
		{
			Dispose(false);
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
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			GC.SuppressFinalize(this);

			ActionUtils.IgnoreExceptions(() => this.AsyncLocalAmbientTransactionContext?.Dispose());
			ActionUtils.IgnoreExceptions(() => this.asyncLocalExecutionVersion.Dispose());
			ActionUtils.IgnoreExceptions(() => this.asyncLocalTransactionalAmbientTransactionContext.Dispose());

			this.IsDisposed = true;
			DisposeAllSqlDatabaseContexts();
			OnDisposed(EventArgs.Empty);
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
			return this.TypeDescriptorProvider.GetTypeDescriptor(GetDefinitionTypeFromConcreteType(type));
		}

		public DataAccessObjectDataContext GetCurrentDataContext(bool forWrite)
		{
			return TransactionContext.GetCurrent(this, forWrite)?.GetCurrentDataContext();
		}
		
		public SqlTransactionalCommandsContext GetCurrentCommandsContext()
		{
			var transactionContext = TransactionContext.GetCurrent(this, true);

			if (transactionContext == null)
			{
				throw new InvalidOperationException("No Current DataAccessScope");
			}
			
			return transactionContext.GetSqlTransactionalCommandsContext();
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
				throw new InvalidOperationException("No configuration specified or declared");
			}

			var buildInfo = CachingDataAccessModelAssemblyProvider.Default.GetDataAccessModelAssembly(dataAccessModelType, configuration);
			var retval = buildInfo.NewDataAccessModel();

			retval.SetConfiguration(configuration);
			retval.SetAssemblyBuildInfo(buildInfo);
			retval.hasAnyAutoIncrementValidators = retval.TypeDescriptorProvider.GetTypeDescriptors().Any(c => c.PersistedProperties.Any(d => d.IsAutoIncrement && d.AutoIncrementAttribute.ValidateExpression != null));

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
			return (T) GetReference(typeof(T), primaryKey);
		}

		protected internal virtual DataAccessObject GetReference<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			var primaryKeyValues = GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			return GetReference(type, primaryKeyValues);
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

			var existing = GetCurrentDataContext(false)?.GetObject(GetConcreteTypeFromDefinitionType(type), objectPropertyAndValues);

			if (existing != null)
			{
				return existing;
			}
			else
			{
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, false);

				retval.ToObjectInternal()
					.SetIsDeflatedReference(true)
					.SetPrimaryKeys(objectPropertyAndValues)
					.ResetModified()
					.FinishedInitializing()
					.SubmitToCache();

				((IDataAccessModelInternal)this).OnHookCreate(retval);

				return retval;
			}
		}

		public virtual T GetReference<T>(LambdaExpression predicate)
			where T : DataAccessObject
		{
			return (T)GetReference(typeof(T), predicate);
		}

		protected internal virtual DataAccessObject GetReference(Type type, LambdaExpression predicate)
		{
			var existing = GetCurrentDataContext(false)?.GetObject(GetConcreteTypeFromDefinitionType(type), predicate);

			if (existing != null)
			{
				return existing;
			}
			else
			{
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, false);

				retval
					.ToObjectInternal()
					.SetIsDeflatedReference(true)
					.SetDeflatedPredicate(predicate)
					.ResetModified()
					.FinishedInitializing()
					.SubmitToCache();

				((IDataAccessModelInternal)this).OnHookCreate(retval);

				return retval;
			}
		}

		protected internal virtual T GetReferenceByPrimaryKeyColumns<T>(object[] columnValues)
			where T : DataAccessObject
		{
			var propertyValues = GetObjectPropertyValuesForPrimaryKeyColumns<T>(columnValues);

			return GetReference<T>(propertyValues);
		}

		protected internal virtual T GetReferenceByPrimaryKeyProperties<T>(object[] primaryKeyValues)
			where T : DataAccessObject
		{
			var propertyValues = GetObjectPropertyValuesForPrimaryKeyProperties<T>(primaryKeyValues);

			return GetReference<T>(propertyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValuesForPrimaryKeyColumns<T>(object[] primaryKeyValues)
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException(nameof(primaryKeyValues));
			}

			if (primaryKeyValues.All(c => c == null))
			{
				return null;
			}

			var objectTypeHandle = typeof(T).TypeHandle;

			if (!this.objectPropertyValuesByColumnValuesFuncByType.TryGetValue(objectTypeHandle, out var func))
			{
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

				var parameter = Expression.Parameter(typeof(object[]));
				var constructor = ConstructorInfoFastRef.ObjectPropertyValueConstructor;

				var index = 0;
				var initializers = new List<Expression>();

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
					Expression convertedValue;
					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

					if (property.PropertyType.IsDataAccessObjectType())
					{
						var columnInfos = QueryBinder.GetPrimaryKeyColumnInfos(this.TypeDescriptorProvider, property.PropertyTypeTypeDescriptor);
						var args = new Expression[columnInfos.Length];

						for (var i = 0; i < columnInfos.Length; i++)
						{
							args[i] = Expression.Convert(Expression.Constant(primaryKeyValues[index + i]), typeof(object));
						}

						index += columnInfos.Length;

						convertedValue = Expression.Call(Expression.Constant(this), MethodInfoFastRef.DataAccessModelGetReferenceByPrimaryKeyColumnsMethod.MakeGenericMethod(property.PropertyType), Expression.NewArrayInit(typeof(object), args));
					}
					else
					{
						var valueExpression = Expression.Convert(Expression.ArrayIndex(parameter, Expression.Constant(index)), typeof(object));
						
						convertedValue = Expression.Call(MethodInfoFastRef.ConvertChangeTypeMethod, valueExpression, Expression.Constant(propertyInfo.PropertyType));
					}

					var newExpression = Expression.New(constructor, Expression.Constant(propertyInfo.PropertyType), Expression.Constant(property.PropertyName), Expression.Constant(property.PersistedName), Expression.Constant(property.PropertyName.GetHashCode()), convertedValue);

					initializers.Add(newExpression);
					index++;
				}

				var body = Expression.NewArrayInit(typeof(ObjectPropertyValue), initializers);

				var lambdaExpression = Expression.Lambda(typeof(Func<object[], ObjectPropertyValue[]>), body, parameter);

				func = (Func<object[], ObjectPropertyValue[]>)lambdaExpression.Compile();
				
				this.objectPropertyValuesByColumnValuesFuncByType = this.objectPropertyValuesByColumnValuesFuncByType.Clone(objectTypeHandle, func);
			}

			return func(primaryKeyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValuesForPrimaryKeyProperties<T>(object[] primaryKeyValues)
		{
			if (primaryKeyValues == null)
			{
				throw new ArgumentNullException(nameof(primaryKeyValues));
			}

			if (primaryKeyValues.All(c => c == null))
			{
				return null;
			}

			var objectTypeHandle = typeof(T).TypeHandle;

			if (!this.objectPropertyValuesByPrimaryKeyValuesFuncByType.TryGetValue(objectTypeHandle, out var func))
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
					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

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

				this.objectPropertyValuesByPrimaryKeyValuesFuncByType = this.objectPropertyValuesByPrimaryKeyValuesFuncByType.Clone(objectTypeHandle, func);
			}

			return func(primaryKeyValues);
		}

		protected internal ObjectPropertyValue[] GetObjectPropertyValues<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (primaryKey == null)
			{
				throw new ArgumentNullException(nameof(primaryKey));
			}

			var key = new Pair<RuntimeTypeHandle, RuntimeTypeHandle>(type.TypeHandle, Type.GetTypeHandle(primaryKey));

			if (!this.objectPropertyValuesByAnonymousKeyFuncByType.TryGetValue(key, out var func))
			{
				var isSimpleKey = false;
				var typeOfPrimaryKey = primaryKey.GetType();
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(type);
				var idPropertyType = typeDescriptor.PrimaryKeyProperties[0].PropertyType;

				if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(typeOfPrimaryKey) || (typeDescriptor.PrimaryKeyCount == 1 && idPropertyType.IsAssignableFrom(typeOfPrimaryKey) && primaryKeyType == PrimaryKeyType.Auto))
				{
					isSimpleKey = true;
				}

				if (isSimpleKey && typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new InvalidOperationException($"Composite primary key expected for type {type} instead of key of type {typeOfPrimaryKey}");
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
						.Where(c => typeOfPrimaryKey.GetMostDerivedProperty(c.PropertyName) != null)
						.ToList();

					replacementPrimaryKeyValues = properties.ToDictionary
					(
						c => c.ComputedMemberAssignTarget.Name,
						c => SqlMemberAccessReplacer.Replace
						(
							c.ComputedMemberAssignmentValue,
							c.PropertyInfo,
							Expression.Convert(Expression.Property(typedParameter, typedParameter.Type.GetMostDerivedProperty(c.PropertyName)), c.PropertyInfo.PropertyType)
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
							valueExpression = Expression.Property(typedParameter, typedParameter.Type.GetMostDerivedProperty(property.PropertyName));
						}
					}

					Expression primaryKeyValue;

					var propertyInfo = DataAccessObjectTypeBuilder.GetPropertyInfo(GetConcreteTypeFromDefinitionType(typeDescriptor.Type), property.PropertyName);

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

				this.objectPropertyValuesByAnonymousKeyFuncByType = this.objectPropertyValuesByAnonymousKeyFuncByType.Clone(key, func);
			}

			return func(primaryKey);
		}

		public virtual T GetReference<T>(object primaryKey)
			where T : DataAccessObject
		{
			return GetReference<T>(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetReference<T>(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
			where T : DataAccessObject
		{
			return (T)GetReference(typeof(T), GetObjectPropertyValues(typeof(T), primaryKey, primaryKeyType));
		}

		public virtual T GetReference<T, K>(K primaryKey)
			where T : DataAccessObject
		{
			return GetReference<T, K>(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetReference<T, K>(K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
			where T : DataAccessObject
		{
			var propertyValues = GetObjectPropertyValues<K>(typeof(T), primaryKey, primaryKeyType);

			return GetReference<T>(propertyValues);
		}

		public virtual DataAccessObject CreateDataAccessObject(Type type)
		{
			var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, true);
			var retvalInternal = retval.ToObjectInternal();

			retvalInternal
				.FinishedInitializing()
				.SubmitToCache();

			((IDataAccessModelInternal)this).OnHookCreate(retval);

			return retval;
		}

		public virtual DataAccessObject CreateDataAccessObject<K>(Type type, K primaryKey)
		{
			return CreateDataAccessObject(type, primaryKey, PrimaryKeyType.Auto);
		}

		public virtual DataAccessObject CreateDataAccessObject<K>(Type type, K primaryKey, PrimaryKeyType primaryKeyType)
		{
			if (!typeof(IDataAccessObjectAdvanced).IsAssignableFrom(type)
				|| !typeof(DataAccessObject<>).IsAssignableFromIgnoreGenericParameters(type))
			{
				throw new ArgumentException("Type must be a DataAccessObjectType", nameof(type));
			}

			var objectPropertyAndValues = GetObjectPropertyValues(type, primaryKey, primaryKeyType);

			if (objectPropertyAndValues.Any(keyValue => keyValue.Value == null))
			{
				throw new MissingOrInvalidPrimaryKeyException();
			}

			var existing = GetCurrentDataContext(false)?.GetObject(GetConcreteTypeFromDefinitionType(type), objectPropertyAndValues);

			if (existing != null)
			{
				IDataAccessObjectAdvanced obj = null;

				ActionUtils.IgnoreExceptions(() => obj = GetReference(type, primaryKey, primaryKeyType));

				throw new ObjectAlreadyExistsException(obj, null, "CreateDataAccessObject");
			}
			else
			{
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject(type, this, true);

				retval.ToObjectInternal()
					.SetPrimaryKeys(objectPropertyAndValues)
					.FinishedInitializing()
					.SubmitToCache();

				((IDataAccessModelInternal)this).OnHookCreate(retval);

				return retval;
			}
		}

		public virtual T CreateDataAccessObject<T>()
			where T : DataAccessObject
		{
			var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject<T>(this, true);
			var retvalInternal = retval.ToObjectInternal();

			retvalInternal.FinishedInitializing();
			retvalInternal.SubmitToCache();

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

			var existing = GetCurrentDataContext(false)?.GetObject(GetConcreteTypeFromDefinitionType(typeof(T)), objectPropertyAndValues);

			if (existing != null)
			{
				T obj = null;

				ActionUtils.IgnoreExceptions(() => obj = GetReference<T, K>(primaryKey, primaryKeyType));

				throw new ObjectAlreadyExistsException(obj, null, "CreateDataAccessObject");
			}
			else
			{
				var retval = this.RuntimeDataAccessModelInfo.CreateDataAccessObject<T>(this, true);

				retval.ToObjectInternal()
					.SetPrimaryKeys(objectPropertyAndValues)
					.FinishedInitializing()
					.SubmitToCache();

				return retval;
			}
		}
		
		public virtual SqlDatabaseContext GetCurrentSqlDatabaseContext()
		{
			var forWrite = DataAccessTransaction.Current != null;

			var transactionContext = GetCurrentContext(forWrite);

			if (transactionContext?.sqlDatabaseContext != null)
			{
				return transactionContext.sqlDatabaseContext;
			}

			var categories = transactionContext?.DatabaseContextCategoriesKey ?? "*";

			if (!this.sqlDatabaseContextsByCategory.TryGetValue(categories, out var info))
			{
				var compositeInfo = SqlDatabaseContextsInfo.Create();

				if (transactionContext != null)
				{
					foreach (var category in transactionContext.DatabaseContextCategoriesKey.Split(",").Select(c => c.Trim()))
					{
						info = this.sqlDatabaseContextsByCategory[category];

						compositeInfo.DatabaseContexts.AddRange(info.DatabaseContexts);
					}
				}

				info = this.sqlDatabaseContextsByCategory[categories] = compositeInfo;
			}

			var index = (int)(info.Count++ % info.DatabaseContexts.Count);
			var retval = info.DatabaseContexts[index];

			if (transactionContext != null)
			{
				transactionContext.sqlDatabaseContext = retval;
			}

			return retval;
		}

		public virtual void SetCurrentTransactionDatabaseCategories(params string[] categories)
		{
			var transactionContext = GetCurrentContext(false);

			if (transactionContext == null)
			{
				throw new InvalidOperationException("No current TransactionContext");
			}

			if (transactionContext.AnyCommandsHaveBeenPerformed())
			{
				throw new InvalidOperationException("Transactions database context categories can only be set before any scope operations are performed");
			}

			var category = categories.FirstOrDefault(c => !this.sqlDatabaseContextsByCategory.ContainsKey(c));

			if (category != null)
			{
				throw new InvalidOperationException("Unsupported category: " + category);
			}

			transactionContext.DatabaseContextCategoriesKey = string.Join(",", categories);
		}

		public virtual void SetCurentTransactionReadOnly()
		{
			SetCurrentTransactionDatabaseCategories("ReadOnly");
		}

		public virtual void CreateIfNotExist()
		{
			Create(DatabaseCreationOptions.IfDatabaseNotExist);
		}
		
		[RewriteAsync]
		public virtual void Create(DatabaseCreationOptions options)
		{
			using (var scope = new DataAccessScope(DataAccessIsolationLevel.Unspecified, DataAccessScopeOptions.RequiresNew, TimeSpan.Zero))
			{
				GetCurrentSqlDatabaseContext().SchemaManager.CreateDatabaseAndSchema(options);

				scope.Complete();
			}
		}

		[RewriteAsync]
		public virtual void Flush()
		{
			var transactionContext = GetCurrentContext(true);
			
			transactionContext?.GetCurrentDataContext().Commit(transactionContext.GetSqlTransactionalCommandsContext(), true);
		}

		protected internal ISqlQueryProvider NewQueryProvider()
		{
			return GetCurrentSqlDatabaseContext().CreateQueryProvider();
		}

		protected internal DataAccessObject Inflate(DataAccessObject dataAccessObject)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException(nameof(dataAccessObject));
			}

			var definitionTypeHandle = dataAccessObject.GetAdvanced().DefinitionType.TypeHandle;

			if (!this.inflateFuncsByType.TryGetValue(definitionTypeHandle, out var func))
			{
				var definitionType = Type.GetTypeFromHandle(definitionTypeHandle);
				var parameter = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "dataAccessObject");
				var methodInfo = MethodInfoFastRef.DataAccessModelGenericInflateHelperMethod.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType));

				var lambda = Expression.Lambda<Func<DataAccessObject, DataAccessObject>>(body, parameter);

				func = lambda.Compile();
				
				this.inflateFuncsByType = this.inflateFuncsByType.Clone(definitionTypeHandle, func);
			}

			return func(dataAccessObject);
		}

		protected internal Task<DataAccessObject> InflateAsync(DataAccessObject dataAccessObject, CancellationToken cancellationToken)
		{
			if (dataAccessObject == null)
			{
				throw new ArgumentNullException(nameof(dataAccessObject));
			}

			var definitionTypeHandle = dataAccessObject.GetAdvanced().DefinitionType.TypeHandle;

			if (!this.inflateAsyncFuncsByType.TryGetValue(definitionTypeHandle, out var func))
			{
				var definitionType = Type.GetTypeFromHandle(definitionTypeHandle);
				var parameter = Expression.Parameter(typeof(IDataAccessObjectAdvanced), "dataAccessObject");
				var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
				var methodInfo = MethodInfoFastRef.DataAccessModelGenericInflateAsyncHelperMethod.MakeGenericMethod(definitionType);
				var body = Expression.Call(Expression.Constant(this), methodInfo, Expression.Convert(parameter, definitionType), cancellationTokenParameter);

				var lambda = Expression.Lambda<Func<DataAccessObject, CancellationToken, Task<DataAccessObject>>>(body, parameter, cancellationTokenParameter);

				func = lambda.Compile();
				
				this.inflateAsyncFuncsByType = this.inflateAsyncFuncsByType.Clone(definitionTypeHandle, func);
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

			var predicate = obj.ToObjectInternal().DeflatedPredicate;

			if (predicate != null)
			{
				var retval = GetDataAccessObjects<T>().SingleOrDefault((Expression<Func<T, bool>>)predicate);

				if (retval == null)
				{
					throw new MissingDataAccessObjectException(obj);
				}

				return retval;
			}
			else
			{
				var retval = GetDataAccessObjects<T>().FirstOrDefault(c => c == obj);

				if (retval == null)
				{
					throw new MissingDataAccessObjectException(obj);
				}

				return retval;
			}
		}

		internal async Task<DataAccessObject> InflateAsyncHelper<T>(T obj, CancellationToken cancellationToken)
			where T : DataAccessObject
		{
			if (!obj.IsDeflatedReference())
			{
				return obj;
			}

			var predicate = obj.ToObjectInternal().DeflatedPredicate;

			if (predicate != null)
			{
				var retval = await GetDataAccessObjects<T>().SingleOrDefaultAsync((Expression<Func<T, bool>>)predicate, cancellationToken);

				if (retval == null)
				{
					throw new MissingDataAccessObjectException(obj);
				}

				return retval;
			}
			else
			{
				var retval = await GetDataAccessObjects<T>().FirstOrDefaultAsync(c => c == obj, cancellationToken);

				if (retval == null)
				{
					throw new MissingDataAccessObjectException(obj);
				}

				return retval;
			}
		}

		public virtual DataAccessObjects<T> ExecuteProcedure<T>(string procedureName, object[] args)
			where T : DataAccessObject
		{
			return null;
		}

		[RewriteAsync]
		public virtual void Backup(DataAccessModel dataAccessModel)
		{
			if (dataAccessModel == this)
			{
				throw new InvalidOperationException("Cannot backup to self");
			}

			GetCurrentSqlDatabaseContext().Backup(dataAccessModel.GetCurrentSqlDatabaseContext());
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
