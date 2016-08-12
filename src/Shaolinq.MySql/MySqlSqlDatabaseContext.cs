// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public partial class MySqlSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string Username { get; }
		public string Password { get; }
		public string ServerName { get; }
		public string SqlMode { get; }

		public static MySqlSqlDatabaseContext Create(MySqlSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDataTypeProvider = new MySqlSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(new MySqlSqlDialect(), sqlDataTypeProvider, typeof(MySqlSqlQueryFormatter));

			return new MySqlSqlDatabaseContext(model, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private MySqlSqlDatabaseContext(DataAccessModel model, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, MySqlSqlDatabaseContextInfo contextInfo)
			: base(model, new MySqlSqlDialect(), sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
		{
			this.SqlMode = contextInfo.SqlMode;
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;

			this.ConnectionString = $"Server={this.ServerName}; Database={this.DatabaseName}; Uid={this.Username}; Pwd={this.Password}; Pooling={contextInfo.PoolConnections}; AutoEnlist=false; charset=utf8; Convert Zero Datetime={(contextInfo.ConvertZeroDateTime ? "true" : "false")}; Allow Zero Datetime={(contextInfo.AllowConvertZeroDateTime ? "true" : "false")};";
			this.ServerConnectionString = Regex.Replace(this.ConnectionString, @"Database\s*\=[^;$]+[;$]", "");

			this.SchemaManager = new MySqlSqlDatabaseSchemaManager(this);
		}

		[RewriteAsync]
		public override IDbConnection OpenConnection()
		{
			var retval = base.OpenConnection();
			
			using (var command = retval.CreateCommand())
			{
				var prefix = this.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
				var parameter = command.CreateParameter();

				parameter.DbType = DbType.String;
				parameter.ParameterName = $"{prefix}param";
				parameter.Value = this.SqlMode ?? "STRICT_ALL_TABLES";

				command.CommandText = $"SET SESSION sql_mode = {prefix}param;";
				command.Parameters.Add(parameter);
				command.ExecuteNonQueryEx(this.DataAccessModel, true);
			}

			return retval;
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new MySqlClientFactory();
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void DropAllConnections()
		{
		}

		public override Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			var mySqlException = exception as MySqlException;

			if (mySqlException == null)
			{
				return base.DecorateException(exception, dataAccessObject, relatedQuery);
			}

			switch (mySqlException.Number)
			{
			case 1062:
				if (mySqlException.Message.Contains("for key 'PRIMARY'"))
				{
					return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
				}
				else
				{
					return new UniqueConstraintException(exception, relatedQuery);
				}
			case 1364:
				return new MissingPropertyValueException(dataAccessObject, mySqlException, relatedQuery);
			case 1451:
				throw new OperationConstraintViolationException((Exception)null, relatedQuery);
			case 1452:
				throw new MissingRelatedDataAccessObjectException(null, dataAccessObject, mySqlException, relatedQuery);
			default:
				return new DataAccessException(exception, relatedQuery);
			}
		}
	}
}
