// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract partial class SqlDatabaseContext
		: IDisposable
	{
		public TimeSpan? CommandTimeout { get; protected set; }

		protected DbProviderFactory dbProviderFactory;
		internal volatile Dictionary<ExpressionCacheKey, ProjectorExpressionCacheInfo> projectionExpressionCache = new Dictionary<ExpressionCacheKey, ProjectorExpressionCacheInfo>(ExpressionCacheKeyEqualityComparer.Default);
		internal volatile Dictionary<ProjectorCacheKey, ProjectorCacheInfo> projectorCache = new Dictionary<ProjectorCacheKey, ProjectorCacheInfo>(ProjectorCacheEqualityComparer.Default);
		internal volatile Dictionary<DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatKey, DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatValue> formattedInsertSqlCache = new Dictionary<DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatKey, DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatValue>(DefaultSqlTransactionalCommandsContext.CommandKeyComparer.Default);
		internal volatile Dictionary<DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatKey, DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatValue> formattedUpdateSqlCache = new Dictionary<DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatKey, DefaultSqlTransactionalCommandsContext.SqlCachedUpdateInsertFormatValue>(DefaultSqlTransactionalCommandsContext.CommandKeyComparer.Default);

		public string DatabaseName { get; }
		public string SchemaName { get; protected set; }
		public string[] ContextCategories { get; protected set; }
		public string TableNamePrefix { get; protected set; }
		public DataAccessModel DataAccessModel { get; }
		public SqlDialect SqlDialect { get; protected set; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; protected set; }
		public SqlDatabaseSchemaManager SchemaManager { get; protected set; }
		public SqlQueryFormatterManager SqlQueryFormatterManager { get; protected set; }
		public string ConnectionString { get; protected set; }
		public string ServerConnectionString { get; protected set; }
		public bool SupportsPreparedTransactions { get; protected set; }

		public abstract DbProviderFactory CreateDbProviderFactory();
		public abstract IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext);

		internal class InjectionContext
		{
			private readonly DataAccessModel model;
			private readonly SqlDatabaseContextInfo contextInfo;
			private readonly Func<SqlDataTypeProvider> defaultProviderFactoryMethod;
			private readonly TypeDescriptorProvider typeDescriptorProvider;
			private readonly ConstraintDefaultsConfiguration constraintsDefaultConfiguration;

			public InjectionContext(DataAccessModel model, SqlDatabaseContextInfo contextInfo, Func<SqlDataTypeProvider> defaultProviderFactoryMethod)
			{
				this.model = model;
				this.contextInfo = contextInfo;
				this.defaultProviderFactoryMethod = defaultProviderFactoryMethod;
				this.typeDescriptorProvider = this.model.TypeDescriptorProvider;
				this.constraintsDefaultConfiguration = this.model.Configuration.ConstraintDefaultsConfiguration;
			}

			public virtual object[] GetArguments(ParameterInfo[] parameterInfos)
			{
				var retval = new object[parameterInfos.Length];

				foreach (var parameter in parameterInfos)
				{
					retval[parameter.Position] = GetArgument(parameter);
				}

				return retval;
			}

			public virtual object GetArgument(ParameterInfo parameterInfo)
			{
				if (parameterInfo.ParameterType == typeof(ConstraintDefaultsConfiguration))
				{
					return this.constraintsDefaultConfiguration;
				}

				if (parameterInfo.ParameterType == typeof(SqlDataTypeProvider))
				{
					return this.defaultProviderFactoryMethod();
				}

				if (parameterInfo.ParameterType == typeof(TypeDescriptorProvider))
				{
					return this.typeDescriptorProvider;
				}

				var property = this.contextInfo.GetType().GetProperty(parameterInfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

				if (property == null)
				{
					throw new InvalidOperationException($"Unable to create resolve value for parameter {parameterInfo.Name} for {parameterInfo.Member.DeclaringType.Name}.{parameterInfo.Member.Name}");
				}

				return Convert.ChangeType(property.GetValue(this.contextInfo), parameterInfo.ParameterType);
			}
		}

		protected static SqlDataTypeProvider CreateSqlDataTypeProvider(DataAccessModel model, SqlDatabaseContextInfo contextInfo, Func<SqlDataTypeProvider> defaultProviderFactoryMethod)
		{
			SqlDataTypeProvider retval = null;

			if (contextInfo.SqlDataTypeProvider != null)
			{
				if (retval == null)
				{
					var constructorInfo = contextInfo.SqlDataTypeProvider.GetConstructor(new[] { typeof(SqlDataTypeProvider) });

					if (constructorInfo != null)
					{
						retval = (SqlDataTypeProvider)constructorInfo.Invoke(new object[] { defaultProviderFactoryMethod() });
					}
				}

				if (retval == null)
				{
					var constructorInfo = contextInfo.SqlDataTypeProvider.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

					if (constructorInfo != null)
					{
						var context = new InjectionContext(model, contextInfo, defaultProviderFactoryMethod);
						var args = context.GetArguments(constructorInfo.GetParameters());

						retval = (SqlDataTypeProvider)constructorInfo.Invoke(args);
					}
				}
			}

			if (retval == null)
			{
				retval = defaultProviderFactoryMethod();
			}

			var defaultSqlDataTypeProvider = retval as DefaultSqlDataTypeProvider;

			if (defaultSqlDataTypeProvider != null && contextInfo.SqlDataTypes?.Count > 0)
			{
				var sqlDataTypeContext = new InjectionContext(model, contextInfo, () => retval);

				foreach (var type in contextInfo.SqlDataTypes)
				{
					var constructors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToList();

					for (var i = 0; i < constructors.Count; i++)
					{
						var constructorInfo = constructors[i];

						var args = sqlDataTypeContext.GetArguments(constructorInfo.GetParameters());

						try
						{
							var sqlDataType = (SqlDataType)constructorInfo.Invoke(args);

							defaultSqlDataTypeProvider.DefineSqlDataType(sqlDataType);

							break;
						}
						catch (InvalidOperationException)
						{
							if (i == constructors.Count - 1)
							{
								throw;
							}
						}
					}
				}
			}
			else if (contextInfo.SqlDataTypes?.Count > 0)
			{
				throw new InvalidOperationException($"Unable to define configured SqlDataTypes because {retval?.GetType().Name} does not extend {nameof(DefaultSqlDataTypeProvider)}");
			}

			return retval;
		}

		[RewriteAsync]
		public SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(TransactionContext transactionContext)
		{
			var connection = this.OpenConnection();

			try
			{
				return this.CreateSqlTransactionalCommandsContext(connection, transactionContext);
			}
			catch
			{
				ActionUtils.IgnoreExceptions(() => connection.Dispose());

				throw;
			}
		}

		protected virtual SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(IDbConnection connection, TransactionContext transactionContext)
		{
			return new DefaultSqlTransactionalCommandsContext(this, connection, transactionContext);
		}

		[RewriteAsync]
		public virtual IDbConnection OpenConnection()
		{
			if (this.dbProviderFactory == null)
			{
				this.dbProviderFactory = this.CreateDbProviderFactory();
			}

			var retval = this.dbProviderFactory.CreateConnection();

			retval.ConnectionString = this.ConnectionString;
			retval.Open();

			return retval;
		}

		[RewriteAsync]
		public virtual IDbConnection OpenServerConnection()
		{
			if (this.dbProviderFactory == null)
			{
				this.dbProviderFactory = this.CreateDbProviderFactory();
			}

			var retval = this.dbProviderFactory.CreateConnection();

			retval.ConnectionString = this.ServerConnectionString;
			retval.Open();

			return retval;
		}

		protected SqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, string databaseName, SqlDatabaseContextInfo contextInfo)
		{
			this.DatabaseName = databaseName;
			this.DataAccessModel = model; 
			this.CommandTimeout = contextInfo.CommandTimeout == null ? null : (TimeSpan?)TimeSpan.FromSeconds(contextInfo.CommandTimeout.Value);
			var categories = contextInfo.Categories ?? "";
			this.ContextCategories = categories.Trim().Length == 0 ? new string[0] : categories.Split(',').Select(c => c.Trim()).ToArray();
			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;
			this.SqlQueryFormatterManager = sqlQueryFormatterManager;
			this.SchemaName = EnvironmentSubstitutor.Substitute(contextInfo.SchemaName);
			this.TableNamePrefix = EnvironmentSubstitutor.Substitute(contextInfo.TableNamePrefix);
		}

		public virtual ISqlQueryProvider CreateQueryProvider()
		{
			return new SqlQueryProvider(this.DataAccessModel, this);
		}
		
		public virtual string GetRelatedSql(Exception e)
		{
			return null;
		}

		public virtual Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			return exception;
		}

		public virtual void DropAllConnections()
		{
		}

		~SqlDatabaseContext()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public virtual void Dispose(bool disposing)
		{
			this.SchemaManager.Dispose();

			GC.SuppressFinalize(this);
		}

		[RewriteAsync]
		public virtual void Backup(SqlDatabaseContext sqlDatabaseContext)
		{
			throw new NotSupportedException();
		}
	}
}
