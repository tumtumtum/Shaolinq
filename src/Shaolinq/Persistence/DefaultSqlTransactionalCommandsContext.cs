// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;
using FormatParamValue = Shaolinq.Persistence.SqlQueryFormatterManager.FormatParamValue;

namespace Shaolinq.Persistence
{
	public partial class DefaultSqlTransactionalCommandsContext
		: SqlTransactionalCommandsContext
	{
		protected static readonly ILog Logger = LogProvider.GetLogger("Shaolinq.Query");

		protected internal struct SqlCachedUpdateInsertFormatValue
		{
			public SqlQueryFormatResult formatResult;
			public int[] valueIndexesToParameterPlaceholderIndexes;
			public int[] primaryKeyIndexesToParameterPlaceholderIndexes;
		}

		protected internal struct SqlCachedUpdateInsertFormatKey
		{
			public readonly Type dataAccessObjectType;
			public readonly IList<ObjectPropertyValue> changedProperties;

			public SqlCachedUpdateInsertFormatKey(Type dataAccessObjectType, IList<ObjectPropertyValue> changedProperties)
			{
				this.dataAccessObjectType = dataAccessObjectType;
				this.changedProperties = changedProperties;
			}
		}

		protected internal class CommandKeyComparer
			: IEqualityComparer<SqlCachedUpdateInsertFormatKey>
		{
			public static readonly CommandKeyComparer Default = new CommandKeyComparer();

			public bool Equals(SqlCachedUpdateInsertFormatKey x, SqlCachedUpdateInsertFormatKey y)
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

			public int GetHashCode(SqlCachedUpdateInsertFormatKey obj)
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
					return new FormatParamValue("(?!)", true);
				}

				return new FormatParamValue(((IDbDataParameter)command.Parameters[c]).Value, true);
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

			parameter.DbType = this.GetDbType(result.Type);
			parameter.Value = result.Value ?? DBNull.Value;
		
			return parameter;
		}

		private void FillParameters(IDbCommand command, SqlCachedUpdateInsertFormatValue cachedValue, IReadOnlyCollection<ObjectPropertyValue> changedProperties, IReadOnlyCollection<ObjectPropertyValue> primaryKeys)
		{
			if (changedProperties == null && primaryKeys == null)
			{
				foreach (var parameter in cachedValue.formatResult.ParameterValues)
				{
					this.AddParameter(command, parameter.Type, parameter.Value);
				}

				return;
			}

			if (cachedValue.formatResult.ParameterValues.Count == (changedProperties?.Count ?? 0) + (primaryKeys?.Count ?? 0))
			{
				foreach (var value in changedProperties)
				{
					this.AddParameter(command, value.PropertyType, value.Value);
				}

				if (primaryKeys != null)
				{
					foreach (var value in primaryKeys)
					{
						this.AddParameter(command, value.PropertyType, value.Value);
					}
				}

				return;
			}

			var newParameters = new List<TypedValue>(cachedValue.formatResult.ParameterValues);
			
			if (changedProperties != null)
			{
				var i = 0;

				foreach (var changed in changedProperties)
				{
					var temp = cachedValue.valueIndexesToParameterPlaceholderIndexes[i];
					int parameterIndex;

					if (cachedValue.formatResult.PlaceholderIndexToParameterIndex.TryGetValue(temp, out parameterIndex))
					{
						var typedValue = newParameters[parameterIndex];

						newParameters[parameterIndex] = typedValue.ChangeValue(changed.Value);
					}

					i++;
				}
			}

			if (primaryKeys != null)
			{
				var i = 0;
				
				foreach (var changed in primaryKeys)
				{
					var temp = cachedValue.primaryKeyIndexesToParameterPlaceholderIndexes[i];
					int parameterIndex;

					if (cachedValue.formatResult.PlaceholderIndexToParameterIndex.TryGetValue(temp, out parameterIndex))
					{
						var typedValue = newParameters[parameterIndex];

						newParameters[parameterIndex] = typedValue.ChangeValue(changed.Value);
					}

					i++;
				}
			}

			foreach (var parameter in newParameters)
			{
				this.AddParameter(command, parameter.Type, parameter.Value);
			}
		}

		protected IDbCommand BuildUpdateCommandForDeflatedPredicated(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject, bool valuesPredicated, bool primaryKeysPredicated, List<ObjectPropertyValue> updatedProperties, ObjectPropertyValue[] primaryKeys)
		{
			var constantPlaceholdersCount = 0;
			var assignments = new List<Expression>();
			var success = false;

			var parameter1 = Expression.Parameter(typeDescriptor.Type);

			foreach (var updated in updatedProperties)
			{
				var value = updated.Value;
				var placeholder = updated.Value as Expression;

				if (placeholder == null)
				{
					placeholder = (Expression)new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(updated.Value, updated.PropertyType.CanBeNull() ? updated.PropertyType : updated.PropertyType.MakeNullable()));
				}

				if (placeholder.Type != updated.PropertyType)
				{
					placeholder = Expression.Convert(placeholder, updated.PropertyType);
				}

				var m = TypeUtils.GetMethod(() => default(DataAccessObject).SetColumnValue(default(string), default(int)))
					.GetGenericMethodDefinition()
					.MakeGenericMethod(typeDescriptor.Type, updated.PropertyType);

				assignments.Add(Expression.Call(null, m, parameter1, Expression.Constant(updated.PersistedName), placeholder));
			}

			var parameter = Expression.Parameter(typeDescriptor.Type);

			if (primaryKeys.Length <= 0)
			{
				throw new InvalidOperationException("Expected more than 1 primary key");
			}

			Expression where = null;

			foreach (var primaryKey in primaryKeys)
			{
				var value = primaryKey.Value;
				var placeholder = primaryKey.Value as Expression;

				if (placeholder == null)
				{
					placeholder = new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(value, primaryKey.PropertyType.CanBeNull() ? primaryKey.PropertyType : primaryKey.PropertyType.MakeNullable()));
				}

				if (placeholder.Type != primaryKey.PropertyType)
				{
					placeholder = Expression.Convert(placeholder, primaryKey.PropertyType);
				}

				var pathComponents = primaryKey.PropertyName.Split('.');
				var propertyExpression = pathComponents.Aggregate<string, Expression>(parameter, Expression.Property);
				var currentExpression = Expression.Equal(propertyExpression, placeholder);

				where = where == null ? currentExpression : Expression.And(where, currentExpression);
			}

			var predicate = Expression.Lambda(where, parameter);
			var method = TypeUtils.GetMethod(() => default(IQueryable<DataAccessObject>).UpdateHelper(default(Expression<Action<DataAccessObject>>)))
				.GetGenericMethodDefinition()
				.MakeGenericMethod(typeDescriptor.Type);

			var source = Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeDescriptor.Type), Expression.Constant(this.DataAccessModel.GetDataAccessObjects(typeDescriptor.Type)), Expression.Quote(predicate));
			var selector = Expression.Lambda(Expression.Block(assignments), parameter1);
			var expression = (Expression)Expression.Call(null, method, source, Expression.Quote(selector));

			expression = SqlQueryProvider.Bind(this.DataAccessModel, this.sqlDataTypeProvider, expression);
			expression = SqlQueryProvider.Optimize(this.DataAccessModel, this.SqlDatabaseContext, expression);
			var projectionExpression = expression as SqlProjectionExpression;

			expression = projectionExpression.Select.From;

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression);

			IDbCommand command = null;

			try
			{
				command = this.CreateCommand();

				command.CommandText = result.CommandText;

				var cachedValue = new SqlCachedUpdateInsertFormatValue { formatResult = result };

				FillParameters(command, cachedValue, null, null);

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

		protected virtual IDbCommand BuildUpdateCommand(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject)
		{
			bool valuesPredicated;
			bool primaryKeysPredicated;
			IDbCommand command = null;
			SqlCachedUpdateInsertFormatValue cachedValue;
			var updatedProperties = dataAccessObject.ToObjectInternal().GetChangedPropertiesFlattened(out valuesPredicated);

			if (updatedProperties.Count == 0)
			{
				return null;
			}
			
			var success = false;
			var primaryKeys = dataAccessObject.ToObjectInternal().GetPrimaryKeysForUpdateFlattened(out primaryKeysPredicated);

			if (valuesPredicated || primaryKeysPredicated)
			{
				return BuildUpdateCommandForDeflatedPredicated(typeDescriptor, dataAccessObject, valuesPredicated, primaryKeysPredicated, updatedProperties, primaryKeys);
			}

			var commandKey = new SqlCachedUpdateInsertFormatKey(dataAccessObject.GetType(), updatedProperties);
		
			if (this.TryGetUpdateCommand(commandKey, out cachedValue))
			{
				try
				{
					command = this.CreateCommand();
					command.CommandText = cachedValue.formatResult.CommandText;

					this.FillParameters(command, cachedValue, updatedProperties, primaryKeys);

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

			var constantPlaceholdersCount = 0;
			var valueIndexesToParameterPlaceholderIndexes = new int[updatedProperties.Count];
			var primaryKeyIndexesToParameterPlaceholderIndexes = new int[primaryKeys.Length];

			var assignments = new List<Expression>(updatedProperties.Count);

			foreach (var updated in updatedProperties)
			{
				var value = (Expression)new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(updated.Value, updated.PropertyType.CanBeNull() ? updated.PropertyType : updated.PropertyType.MakeNullable()));

				if (value.Type != updated.PropertyType)
				{
					value = Expression.Convert(value, updated.PropertyType);
				}

				assignments.Add(new SqlAssignExpression(new SqlColumnExpression(updated.PropertyType, null, updated.PersistedName), value));
			}
			
			Expression where = null;

			Debug.Assert(primaryKeys.Length > 0);

			foreach (var primaryKey in primaryKeys)
			{
				var value = primaryKey.Value;
				var placeholder = (Expression)new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(value, primaryKey.PropertyType.CanBeNull() ? primaryKey.PropertyType : primaryKey.PropertyType.MakeNullable()));

				if (placeholder.Type != primaryKey.PropertyType)
				{
					placeholder = Expression.Convert(placeholder, primaryKey.PropertyType);
				}

				var currentExpression = Expression.Equal(new SqlColumnExpression(primaryKey.PropertyType, null, primaryKey.PersistedName), placeholder);
				
				where = where == null ? currentExpression : Expression.And(where, currentExpression);
			}

			for (var i = 0; i < assignments.Count; i++)
			{
				valueIndexesToParameterPlaceholderIndexes[i] = i;
			}

			for (var i = 0; i < primaryKeys.Length; i++)
			{
				primaryKeyIndexesToParameterPlaceholderIndexes[i] = i + assignments.Count;
			}

			var expression = (Expression)new SqlUpdateExpression(new SqlTableExpression(typeDescriptor.PersistedName), assignments, where);

			expression = SqlObjectOperandComparisonExpander.Expand(expression);

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default);

			try
			{
				command = this.CreateCommand();
				command.CommandText = result.CommandText;

				cachedValue = new SqlCachedUpdateInsertFormatValue { formatResult = result, valueIndexesToParameterPlaceholderIndexes = valueIndexesToParameterPlaceholderIndexes, primaryKeyIndexesToParameterPlaceholderIndexes = primaryKeyIndexesToParameterPlaceholderIndexes };

				if (result.Cacheable)
				{
					this.CacheUpdateCommand(commandKey, cachedValue);
				}

				FillParameters(command, cachedValue, null, null);
				
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

		protected IDbCommand BuildInsertCommandForDeflatedPredicated(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject, List<ObjectPropertyValue> updatedProperties)
		{
			var constantPlaceholdersCount = 0;
			var assignments = new List<Expression>();
			var success = false;

			var parameter1 = Expression.Parameter(typeDescriptor.Type);

			foreach (var updated in updatedProperties)
			{
				var placeholder = updated.Value as Expression ?? new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(updated.Value, updated.PropertyType.CanBeNull() ? updated.PropertyType : updated.PropertyType.MakeNullable()));

				if (placeholder.Type != updated.PropertyType)
				{
					placeholder = Expression.Convert(placeholder, updated.PropertyType);
				}

				var m = TypeUtils.GetMethod(() => default(DataAccessObject).SetColumnValue(default(string), default(int)))
					.GetGenericMethodDefinition()
					.MakeGenericMethod(typeDescriptor.Type, updated.PropertyType);

				assignments.Add(Expression.Call(null, m, parameter1, Expression.Constant(updated.PersistedName), placeholder));
			}

			var method = TypeUtils.GetMethod(() => default(IQueryable<DataAccessObject>).InsertHelper(default(Expression<Action<DataAccessObject>>)))
				.GetGenericMethodDefinition()
				.MakeGenericMethod(typeDescriptor.Type);

			var source = Expression.Constant(this.DataAccessModel.GetDataAccessObjects(typeDescriptor.Type));
			var selector = Expression.Lambda(Expression.Block(assignments), parameter1);
			var expression = (Expression)Expression.Call(null, method, source, Expression.Quote(selector));

			expression = SqlQueryProvider.Bind(this.DataAccessModel, this.sqlDataTypeProvider, expression);
			expression = SqlQueryProvider.Optimize(this.DataAccessModel, this.SqlDatabaseContext, expression);
			var projectionExpression = expression as SqlProjectionExpression;

			expression = projectionExpression.Select.From;

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

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression);

			IDbCommand command = null;

			try
			{
				command = this.CreateCommand();
				command.CommandText = result.CommandText;

				var cachedValue = new SqlCachedUpdateInsertFormatValue { formatResult = result };

				FillParameters(command, cachedValue, null, null);

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
			SqlCachedUpdateInsertFormatValue cachedValue;
			bool predicated;

			var updatedProperties = dataAccessObject.ToObjectInternal().GetChangedPropertiesFlattened(out predicated);

			if (predicated)
			{
				return BuildInsertCommandForDeflatedPredicated(typeDescriptor, dataAccessObject, updatedProperties);
			}

			var commandKey = new SqlCachedUpdateInsertFormatKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetInsertCommand(commandKey, out cachedValue))
			{
				try
				{
					command = this.CreateCommand();
					command.CommandText = cachedValue.formatResult.CommandText;
					this.FillParameters(command, cachedValue, updatedProperties, null);

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
				var propertyDescriptors = typeDescriptor.PersistedPropertiesWithoutBackreferences.Where(c => c.IsPropertyThatIsCreatedOnTheServerSide).ToList();

				returningAutoIncrementColumnNames = propertyDescriptors.Select(c => c.PersistedName).ToReadOnlyCollection();
			}

			var constantPlaceholdersCount = 0;
			var valueIndexesToParameterPlaceholderIndexes = new int[updatedProperties.Count];
			
			var columnNames = updatedProperties.Select(c => c.PersistedName).ToReadOnlyCollection();
			
			var valueExpressions = new List<Expression>(updatedProperties.Count);

			foreach (var updated in updatedProperties)
			{
				var value = (Expression)new SqlConstantPlaceholderExpression(constantPlaceholdersCount++, Expression.Constant(updated.Value, updated.PropertyType.CanBeNull() ? updated.PropertyType : updated.PropertyType.MakeNullable()));

				if (value.Type != updated.PropertyType)
				{
					value = Expression.Convert(value, updated.PropertyType);
				}

				valueExpressions.Add(value);
			}

			Expression expression = new SqlInsertIntoExpression(new SqlTableExpression(typeDescriptor.PersistedName), columnNames, returningAutoIncrementColumnNames, valueExpressions);

			for (var i = 0; i < constantPlaceholdersCount; i++)
			{
				valueIndexesToParameterPlaceholderIndexes[i] = i;
			}

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

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression);

			try
			{
				command = this.CreateCommand();

				var commandText = result.CommandText;

				command.CommandText = commandText;
				cachedValue = new SqlCachedUpdateInsertFormatValue { formatResult = result, valueIndexesToParameterPlaceholderIndexes = valueIndexesToParameterPlaceholderIndexes, primaryKeyIndexesToParameterPlaceholderIndexes = null };

				if (result.Cacheable)
				{
					this.CacheInsertCommand(commandKey, cachedValue);
				}

				this.FillParameters(command, cachedValue, updatedProperties, null);

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

		protected void CacheInsertCommand(SqlCachedUpdateInsertFormatKey sqlCachedUpdateInsertFormatKey, SqlCachedUpdateInsertFormatValue sqlCachedUpdateInsertFormatValue)
		{
			var newDictionary = new Dictionary<SqlCachedUpdateInsertFormatKey, SqlCachedUpdateInsertFormatValue>(this.SqlDatabaseContext.formattedInsertSqlCache, CommandKeyComparer.Default) { [sqlCachedUpdateInsertFormatKey] = sqlCachedUpdateInsertFormatValue };
			
			this.SqlDatabaseContext.formattedInsertSqlCache = newDictionary;
		}

		protected bool TryGetInsertCommand(SqlCachedUpdateInsertFormatKey sqlCachedUpdateInsertFormatKey, out SqlCachedUpdateInsertFormatValue sqlCachedUpdateInsertFormatValue)
		{
			return this.SqlDatabaseContext.formattedInsertSqlCache.TryGetValue(sqlCachedUpdateInsertFormatKey, out sqlCachedUpdateInsertFormatValue);
		}

		protected void CacheUpdateCommand(SqlCachedUpdateInsertFormatKey sqlCachedUpdateInsertFormatKey, SqlCachedUpdateInsertFormatValue sqlCachedUpdateInsertFormatValue)
		{
			var newDictionary = new Dictionary<SqlCachedUpdateInsertFormatKey, SqlCachedUpdateInsertFormatValue>(this.SqlDatabaseContext.formattedUpdateSqlCache, CommandKeyComparer.Default) { [sqlCachedUpdateInsertFormatKey] = sqlCachedUpdateInsertFormatValue };
			
			this.SqlDatabaseContext.formattedUpdateSqlCache = newDictionary;
		}

		protected bool TryGetUpdateCommand(SqlCachedUpdateInsertFormatKey sqlCachedUpdateInsertFormatKey, out SqlCachedUpdateInsertFormatValue sqlCachedUpdateInsertFormatValue)
		{
			return this.SqlDatabaseContext.formattedUpdateSqlCache.TryGetValue(sqlCachedUpdateInsertFormatKey, out sqlCachedUpdateInsertFormatValue);
		}
	}
}
