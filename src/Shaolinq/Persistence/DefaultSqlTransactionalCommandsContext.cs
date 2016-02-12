// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using Platform;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.Persistence
{
	public partial class DefaultSqlTransactionalCommandsContext
		: SqlTransactionalCommandsContext
	{
		protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

		protected internal struct SqlCommandValue
		{
			public string commandText;
		}

		protected internal struct SqlCommandKey
		{
			public readonly Type dataAccessObjectType;
			public readonly IList<ObjectPropertyValue> changedProperties;

			public SqlCommandKey(Type dataAccessObjectType, IList<ObjectPropertyValue> changedProperties)
			{
				this.dataAccessObjectType = dataAccessObjectType;
				this.changedProperties = changedProperties;
			}
		}

		protected internal class CommandKeyComparer
			: IEqualityComparer<SqlCommandKey>
		{
			public static readonly CommandKeyComparer Default = new CommandKeyComparer();

			public bool Equals(SqlCommandKey x, SqlCommandKey y)
			{
				if (x.dataAccessObjectType != y.dataAccessObjectType)
				{
					return false;
				}

				if (x.changedProperties.Count != y.changedProperties.Count)
				{
					return false;
				}

				for (int i = 0, n = x.changedProperties.Count; i < n; i++)
				{
					if (!ReferenceEquals(x.changedProperties[i].PersistedName, y.changedProperties[i].PersistedName))
					{
						return false;
					}
				}

				return true;
			}

			public int GetHashCode(SqlCommandKey obj)
			{
				var count = obj.changedProperties.Count;
				var retval = obj.dataAccessObjectType.GetHashCode() ^ count;

				if (count > 0)
				{
					retval ^= obj.changedProperties[0].PropertyNameHashCode;

					if (count > 1)
					{
						retval ^= obj.changedProperties[count - 1].PropertyNameHashCode;
					}
				}

				return retval;
			}
		}

		protected readonly string tableNamePrefix;
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		protected readonly string parameterIndicatorPrefix;
		
		public DefaultSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, DataAccessTransaction transaction)
			: base(sqlDatabaseContext, sqlDatabaseContext.OpenConnection(), transaction)
		{
			this.sqlDataTypeProvider = sqlDatabaseContext.SqlDataTypeProvider;
			this.tableNamePrefix = sqlDatabaseContext.TableNamePrefix;
			this.parameterIndicatorPrefix = sqlDatabaseContext.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
		}

		internal string FormatCommand(IDbCommand command)
		{
			return this.SqlDatabaseContext.SqlQueryFormatterManager.Format(command.CommandText, c =>
			{
				if (!command.Parameters.Contains(c))
				{
					return "(?!)";
				}

				return ((IDbDataParameter)command.Parameters[c]).Value;
			});
		}

		protected virtual DbType GetDbType(Type type)
		{
			type = type.GetUnwrappedNullableType();

			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				return DbType.Boolean;
			case TypeCode.Byte:
			case TypeCode.SByte:
				return DbType.Byte;
			case TypeCode.Char:
				return DbType.Object;
			case TypeCode.DateTime:
				return DbType.DateTime;
			case TypeCode.Decimal:
				return DbType.Decimal;
			case TypeCode.Single:
				return DbType.Single;
			case TypeCode.Double:
				return DbType.Double;
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return DbType.Int16;
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return DbType.Int32;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return DbType.Int64;
			case TypeCode.String:
				return DbType.AnsiString;
			default:
				if (type == typeof(Guid))
				{
					return DbType.Guid;
				}
				else if (type.IsArray && type.GetElementType() == typeof(byte))
				{
					return DbType.Binary;
				}
				else if (type.IsEnum)
				{
					return DbType.AnsiString;
				}

				return DbType.Object;
			}
		}

		private Exception LogAndDecorateException(Exception e, IDbCommand command)
		{
			var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
			var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

			Logger.Error(this.FormatCommand(command));
			Logger.Error(e.ToString());

			if (decoratedException != e)
			{
				return decoratedException;
			}

			return null;
		}

		private Dictionary<RuntimeTypeHandle, Func<DataAccessObject, IDataReader, DataAccessObject>> serverSideGeneratedPropertySettersByType = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, IDataReader, DataAccessObject>>();

		private DataAccessObject ApplyPropertiesGeneratedOnServerSide(DataAccessObject dataAccessObject, IDataReader reader)
		{
			if (!dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
			{
				return dataAccessObject;
			}

			Func<DataAccessObject, IDataReader, DataAccessObject> applicator;

			if (!this.serverSideGeneratedPropertySettersByType.TryGetValue(Type.GetTypeHandle(dataAccessObject), out applicator))
			{
				var objectParameter = Expression.Parameter(typeof(DataAccessObject));
				var readerParameter = Expression.Parameter(typeof(IDataReader));
				var propertiesGeneratedOnServerSide = dataAccessObject.GetAdvanced().GetPropertiesGeneratedOnTheServerSide();
				var local = Expression.Variable(dataAccessObject.GetType());

				var statements = new List<Expression>
				{
					Expression.Assign(local, Expression.Convert(objectParameter, dataAccessObject.GetType()))
				};
				
				var index = 0;

				foreach (var property in propertiesGeneratedOnServerSide)
				{
					var sqlDataType = this.sqlDataTypeProvider.GetSqlDataType(property.PropertyType);
					var valueExpression = sqlDataType.GetReadExpression(readerParameter, index++);
					var member = dataAccessObject.GetType().GetProperty(property.PropertyName);

					statements.Add(Expression.Assign(Expression.MakeMemberAccess(local, member), valueExpression));
				}
				
				statements.Add(objectParameter);
				
				var body = Expression.Block(new [] { local }, statements);

				var lambda = Expression.Lambda<Func<DataAccessObject, IDataReader, DataAccessObject>>(body, objectParameter, readerParameter);
				
				applicator = lambda.Compile();

				var newDictionary = new Dictionary<RuntimeTypeHandle, Func<DataAccessObject, IDataReader, DataAccessObject>>(this.serverSideGeneratedPropertySettersByType)
				{
					[Type.GetTypeHandle(dataAccessObject)] = applicator
				};

				this.serverSideGeneratedPropertySettersByType = newDictionary;
			}

			return applicator(dataAccessObject, reader);
		}

		private IDbDataParameter AddParameter(IDbCommand command, Type type, object value)
		{
			var parameter = this.CreateParameter(command, this.parameterIndicatorPrefix + Sql92QueryFormatter.ParamNamePrefix + command.Parameters.Count, type, value);

			command.Parameters.Add(parameter);

			return parameter;
		}

		protected virtual IDbDataParameter CreateParameter(IDbCommand command, string parameterName, Type type, object value)
		{
			var parameter = command.CreateParameter();
		
			parameter.ParameterName = parameterName;

			if (value == null)
			{
				parameter.DbType = this.GetDbType(type);
			}

			var result = this.sqlDataTypeProvider.GetSqlDataType(type).ConvertForSql(value);

			parameter.DbType = this.GetDbType(result.Item1);
			parameter.Value = result.Item2 ?? DBNull.Value;
		
			return parameter;
		}

		private void FillParameters(IDbCommand command, IEnumerable<ObjectPropertyValue> changedProperties, ObjectPropertyValue[] primaryKeys)
		{
			foreach (var infoAndValue in changedProperties)
			{
				this.AddParameter(command, infoAndValue.PropertyType, infoAndValue.Value);
			}

			if (primaryKeys != null)
			{
				foreach (var infoAndValue in primaryKeys)
				{
					this.AddParameter(command, infoAndValue.PropertyType, infoAndValue.Value);
				}
			}
		}

		protected virtual IDbCommand BuildUpdateCommand(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject)
		{
			IDbCommand command = null;
			SqlCommandValue sqlCommandValue;
			var updatedProperties = dataAccessObject.GetAdvanced().GetChangedPropertiesFlattened();
			
			if (updatedProperties.Count == 0)
			{
				return null;
			}

			var success = false;
			var primaryKeys = dataAccessObject.GetAdvanced().GetPrimaryKeysForUpdateFlattened();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetUpdateCommand(commandKey, out sqlCommandValue))
			{
				try
				{
					command = this.CreateCommand();

					command.CommandText = sqlCommandValue.commandText;
					this.FillParameters(command, updatedProperties, primaryKeys);

					success = true;

					return command;
				}
				finally
				{
					if (!success)
					{
						command?.Dispose();
					}
				}
			}

			var assignments = updatedProperties.Select(c => (Expression)new SqlAssignExpression(new SqlColumnExpression(c.PropertyType, null, c.PersistedName), Expression.Constant(c.Value))).ToReadOnlyCollection();

			Expression where = null;

			Debug.Assert(primaryKeys.Length > 0);

			foreach (var primaryKey in primaryKeys)
			{
				var currentExpression = Expression.Equal(new SqlColumnExpression(primaryKey.PropertyType, null, primaryKey.PersistedName), Expression.Constant(primaryKey.Value));
				
				where = where == null ? currentExpression : Expression.And(where, currentExpression);
			}

			var expression = new SqlUpdateExpression(new SqlTableExpression(typeDescriptor.PersistedName), assignments, where);

			expression = (SqlUpdateExpression)SqlObjectOperandComparisonExpander.Expand(expression);

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default & ~SqlQueryFormatterOptions.OptimiseOutConstantNulls);

			try
			{
				command = this.CreateCommand();

				command.CommandText = result.CommandText;
				this.CacheUpdateCommand(commandKey, new SqlCommandValue() { commandText = command.CommandText });
				this.FillParameters(command, updatedProperties, primaryKeys);

				success = true;

				return command;
			}
			finally
			{
				if (!success)
				{
					command?.Dispose();
				}
			}
		}

		protected virtual IDbCommand BuildInsertCommand(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject)
		{
			var success = false;
			IDbCommand command = null;
			SqlCommandValue sqlCommandValue;

			var updatedProperties = dataAccessObject.GetAdvanced().GetChangedPropertiesFlattened();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetInsertCommand(commandKey, out sqlCommandValue))
			{
				try
				{
					command = this.CreateCommand();
					command.CommandText = sqlCommandValue.commandText;
					this.FillParameters(command, updatedProperties, null);

					success = true;

					return command;
				}
				finally
				{
					if (!success)
					{
						command?.Dispose();
					}
				}
			}

			IReadOnlyList<string> returningAutoIncrementColumnNames = null;

			if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
			{
				var propertyDescriptors = typeDescriptor.PersistedProperties.Where(c => c.IsPropertyThatIsCreatedOnTheServerSide).ToList();

				returningAutoIncrementColumnNames = propertyDescriptors.Select(c => c.PersistedName).ToReadOnlyCollection();
			}

			var columnNames = updatedProperties.Select(c => c.PersistedName).ToReadOnlyCollection();
			var valueExpressions = updatedProperties.Select(c => (Expression)Expression.Constant(c.Value)).ToReadOnlyCollection();
			Expression expression = new SqlInsertIntoExpression(new SqlTableExpression(typeDescriptor.PersistedName), columnNames, returningAutoIncrementColumnNames, valueExpressions);

			if (this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.PragmaIdentityInsert) && dataAccessObject.ToObjectInternal().HasAnyChangedPrimaryKeyServerSideProperties)
			{
				var list = new List<Expression>
				{
					new SqlSetCommandExpression("IdentityInsert", new SqlTableExpression(typeDescriptor.PersistedName), Expression.Constant(true)),
					expression,
					new SqlSetCommandExpression("IdentityInsert", new SqlTableExpression(typeDescriptor.PersistedName), Expression.Constant(false)),
				};

				expression = new SqlStatementListExpression(list);
			}

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default & ~SqlQueryFormatterOptions.OptimiseOutConstantNulls);

			Debug.Assert(result.ParameterValues.Count == updatedProperties.Count);

			try
			{
				command = this.CreateCommand();

				var commandText = result.CommandText;

				command.CommandText = commandText;
				this.CacheInsertCommand(commandKey, new SqlCommandValue { commandText = command.CommandText });
				this.FillParameters(command, updatedProperties, null);

				success = true;

				return command;
			}
			finally
			{
				if (!success)
				{
					command?.Dispose();
				}
			}
		}

		protected void CacheInsertCommand(SqlCommandKey sqlCommandKey, SqlCommandValue sqlCommandValue)
		{
			var newDictionary = new Dictionary<SqlCommandKey, SqlCommandValue>(this.SqlDatabaseContext.formattedInsertSqlCache, CommandKeyComparer.Default) { [sqlCommandKey] = sqlCommandValue };
			
			this.SqlDatabaseContext.formattedInsertSqlCache = newDictionary;
		}

		protected bool TryGetInsertCommand(SqlCommandKey sqlCommandKey, out SqlCommandValue sqlCommandValue)
		{
			return this.SqlDatabaseContext.formattedInsertSqlCache.TryGetValue(sqlCommandKey, out sqlCommandValue);
		}

		protected void CacheUpdateCommand(SqlCommandKey sqlCommandKey, SqlCommandValue sqlCommandValue)
		{
			var newDictionary = new Dictionary<SqlCommandKey, SqlCommandValue>(this.SqlDatabaseContext.formattedUpdateSqlCache, CommandKeyComparer.Default) { [sqlCommandKey] = sqlCommandValue };
			
			this.SqlDatabaseContext.formattedUpdateSqlCache = newDictionary;
		}

		protected bool TryGetUpdateCommand(SqlCommandKey sqlCommandKey, out SqlCommandValue sqlCommandValue)
		{
			return this.SqlDatabaseContext.formattedUpdateSqlCache.TryGetValue(sqlCommandKey, out sqlCommandValue);
		}
		
		public static MethodInfo GetDeleteMethod(Type type)
		{
			return DeleteHelperMethod.MakeGenericMethod(type);
		}

		internal static void DeleteHelper<T>(T type, Expression<Func<T, bool>> condition)
		{
		}
	}
}
