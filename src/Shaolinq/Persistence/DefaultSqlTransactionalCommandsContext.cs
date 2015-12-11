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
	public class DefaultSqlTransactionalCommandsContext
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
		internal static readonly MethodInfo DeleteHelperMethod = typeof(DefaultSqlTransactionalCommandsContext).GetMethod("DeleteHelper", BindingFlags.Static | BindingFlags.NonPublic);
		protected readonly string parameterIndicatorPrefix;

		public DefaultSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
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

		public virtual IDataReader ExecuteReader(string sql, IEnumerable<Tuple<Type, object>> parameters)
		{
			var command = this.CreateCommand();
            
			foreach (var value in parameters)
			{
				this.AddParameter(command, value.Item1, value.Item2);
			}
            
			command.CommandText = sql;

			Logger.Debug(() => this.FormatCommand(command));
			
			try
			{
				return command.ExecuteReader();
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

				Logger.Error(this.FormatCommand(command));
				Logger.ErrorFormat(e.ToString());

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}
		}

		public virtual object ExecuteScalar(string sql, IEnumerable<Tuple<Type, object>> parameters)
		{
			var command = this.CreateCommand();

			command.CommandText = sql;

			foreach (var parameter in parameters)
			{
				command.Parameters.Add(parameter.Item2);
			}
			
			Logger.Debug(() => this.FormatCommand(command));

			try
			{
				return command.ExecuteScalar();
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

				Logger.ErrorFormat(e.ToString());

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}
		}

		public override void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				if ((objectState & (ObjectState.Changed | ObjectState.ServerSidePropertiesHydrated)) == 0)
				{
					continue;
				}

				var command = this.BuildUpdateCommand(typeDescriptor, dataAccessObject);

				if (command == null)
				{
					Logger.ErrorFormat("Object is reported as changed but GetChangedProperties returns an empty list ({0})", dataAccessObject);

					continue;
				}

				Logger.Debug(() => this.FormatCommand(command));

				int result;

				try
				{
					result = command.ExecuteNonQuery();
				}
				catch (Exception e)
				{
					var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
					var decoratedException = this.SqlDatabaseContext.DecorateException(e, dataAccessObject, relatedSql);

					Logger.ErrorFormat(e.ToString());

					if (decoratedException != e)
					{
						throw decoratedException;
					}

					throw;
				}

				if (result == 0)
				{
					throw new MissingDataAccessObjectException(dataAccessObject, null, command.CommandText);
				}

				dataAccessObject.ToObjectInternal().ResetModified();
			}
		}

		public override InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var listToFixup = new List<DataAccessObject>();
			var listToRetry = new List<DataAccessObject>();

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				switch (objectState & ObjectState.NewChanged)
				{
				case ObjectState.Unchanged:
					continue;
				case ObjectState.New:
				case ObjectState.NewChanged:
					break;
				case ObjectState.Changed:
					throw new NotSupportedException("Changed state not supported");
				}

				var primaryKeyIsCompelte = (objectState & ObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) == 0;
				var deferrableOrNotReferencingNewObject = (this.SqlDatabaseContext.SqlDialect.SupportsFeature(SqlFeature.Deferrability) || ((objectState & ObjectState.ReferencesNewObject) == 0));
				
				var objectReadyToBeCommited = primaryKeyIsCompelte && deferrableOrNotReferencingNewObject;
				
				if (objectReadyToBeCommited)
				{
					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
					var command = this.BuildInsertCommand(typeDescriptor, dataAccessObject);

					Logger.Debug(() => this.FormatCommand(command));
					
					try
					{
						using (var reader = command.ExecuteReader())
						{
							if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
							{
								var dataAccessObjectInternal = dataAccessObject.ToObjectInternal();

								if (reader.Read())
								{
									this.ApplyPropertiesGeneratedOnServerSide(dataAccessObject, reader);
									dataAccessObjectInternal.MarkServerSidePropertiesAsApplied();
								}

								reader.Close();

								if (dataAccessObjectInternal.ComputeServerGeneratedIdDependentComputedTextProperties())
								{
									this.Update(dataAccessObject.GetType(), new[] {dataAccessObject});
								}
							}
						}
					}
					catch (Exception e)
					{
						var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
						var decoratedException = this.SqlDatabaseContext.DecorateException(e, dataAccessObject, relatedSql);

						Logger.ErrorFormat(e.ToString());

						if (decoratedException != e)
						{
							throw decoratedException;
						}

						throw;
					}

					if ((objectState & ObjectState.ReferencesNewObjectWithServerSideProperties) == ObjectState.ReferencesNewObjectWithServerSideProperties)
					{
						listToFixup.Add(dataAccessObject);
					}
					else
					{
						dataAccessObject.ToObjectInternal().ResetModified();
					}
				}
				else
				{
					listToRetry.Add(dataAccessObject);
 				}
			}

			return new InsertResults(listToFixup, listToRetry);
		}

		private Dictionary<Type, Func<DataAccessObject, IDataReader, DataAccessObject>> serverSideGeneratedPropertySettersByType = new Dictionary<Type, Func<DataAccessObject, IDataReader, DataAccessObject>>();

		private DataAccessObject ApplyPropertiesGeneratedOnServerSide(DataAccessObject dataAccessObject, IDataReader reader)
		{
			if (!dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
			{
				return dataAccessObject;
			}

			Func<DataAccessObject, IDataReader, DataAccessObject> applicator;

			if (!this.serverSideGeneratedPropertySettersByType.TryGetValue(dataAccessObject.GetType(), out applicator))
			{
				var objectParameter = Expression.Parameter(typeof(DataAccessObject));
				var readerParameter = Expression.Parameter(typeof(IDataReader));
				var propertiesGeneratedOnServerSide = dataAccessObject.GetAdvanced().GetPropertiesGeneratedOnTheServerSide();
				var local = Expression.Variable(dataAccessObject.GetType());
				
				var statements = new List<Expression>();

				statements.Add(Expression.Assign(local, Expression.Convert(objectParameter, dataAccessObject.GetType())));

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

				var newDictionary = new Dictionary<Type, Func<DataAccessObject, IDataReader, DataAccessObject>>(this.serverSideGeneratedPropertySettersByType);

				newDictionary[dataAccessObject.GetType()] = applicator;

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

		private void FillParameters(IDbCommand command, IList<ObjectPropertyValue> changedProperties, ObjectPropertyValue[] primaryKeys)
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
			IDbCommand command;
			SqlCommandValue sqlCommandValue;
			var updatedProperties = dataAccessObject.GetAdvanced().GetChangedPropertiesFlattened();
			
			if (updatedProperties.Count == 0)
			{
				return null;
			}

			var primaryKeys = dataAccessObject.GetAdvanced().GetPrimaryKeysForUpdateFlattened();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetUpdateCommand(commandKey, out sqlCommandValue))
			{
				command = this.CreateCommand();
				command.CommandText = sqlCommandValue.commandText;
				this.FillParameters(command, updatedProperties, primaryKeys);

				return command;
			}

			var assignments = updatedProperties.Select(c => (Expression)new SqlAssignExpression(new SqlColumnExpression(c.PropertyType, null, c.PersistedName), Expression.Constant(c.Value))).ToReadOnlyCollection();

			Expression where = null;

			var i = 0;

			Debug.Assert(primaryKeys.Length > 0);

			foreach (var primaryKey in primaryKeys)
			{
				var currentExpression = Expression.Equal(new SqlColumnExpression(primaryKey.PropertyType, null, primaryKey.PersistedName), Expression.Constant(primaryKey.Value));
				
				if (where == null)
				{
					where = currentExpression;
				}
				else
				{
					where = Expression.And(where, currentExpression);
				}

				i++;
			}

			var expression = new SqlUpdateExpression(new SqlTableExpression(typeDescriptor.PersistedName), assignments, where);

			expression = (SqlUpdateExpression)SqlObjectOperandComparisonExpander.Expand(expression);

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default & ~SqlQueryFormatterOptions.OptimiseOutConstantNulls);

			command = this.CreateCommand();

			command.CommandText = result.CommandText;
			this.CacheUpdateCommand(commandKey, new SqlCommandValue() { commandText = command.CommandText });
			this.FillParameters(command, updatedProperties, primaryKeys);
			
			return command;
		}

		protected virtual IDbCommand BuildInsertCommand(TypeDescriptor typeDescriptor, DataAccessObject dataAccessObject)
		{
			IDbCommand command;
			SqlCommandValue sqlCommandValue;

			var updatedProperties = dataAccessObject.GetAdvanced().GetChangedPropertiesFlattened();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetInsertCommand(commandKey, out sqlCommandValue))
			{
				command = this.CreateCommand();
				command.CommandText = sqlCommandValue.commandText;
				this.FillParameters(command, updatedProperties, null);

				return command;
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

			if (this.SqlDatabaseContext.SqlDialect.SupportsFeature(SqlFeature.PragmaIdentityInsert) && dataAccessObject.ToObjectInternal().HasAnyChangedPrimaryKeyServerSideProperties)
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

			Debug.Assert(result.ParameterValues.Count() == updatedProperties.Count);

			command = this.CreateCommand();

			var commandText = result.CommandText;

			command.CommandText = commandText;
			this.CacheInsertCommand(commandKey, new SqlCommandValue { commandText = command.CommandText });
			this.FillParameters(command, updatedProperties, null);

			return command;
		}

		protected void CacheInsertCommand(SqlCommandKey sqlCommandKey, SqlCommandValue sqlCommandValue)
		{
			var newDictionary = new Dictionary<SqlCommandKey, SqlCommandValue>(this.SqlDatabaseContext.formattedInsertSqlCache, CommandKeyComparer.Default);

			newDictionary[sqlCommandKey] = sqlCommandValue;

			this.SqlDatabaseContext.formattedInsertSqlCache = newDictionary;
		}

		protected bool TryGetInsertCommand(SqlCommandKey sqlCommandKey, out SqlCommandValue sqlCommandValue)
		{
			return this.SqlDatabaseContext.formattedInsertSqlCache.TryGetValue(sqlCommandKey, out sqlCommandValue);
		}

		protected void CacheUpdateCommand(SqlCommandKey sqlCommandKey, SqlCommandValue sqlCommandValue)
		{
			var newDictionary = new Dictionary<SqlCommandKey, SqlCommandValue>(this.SqlDatabaseContext.formattedUpdateSqlCache, CommandKeyComparer.Default);

			newDictionary[sqlCommandKey] = sqlCommandValue;

			this.SqlDatabaseContext.formattedUpdateSqlCache = newDictionary;
		}

		protected bool TryGetUpdateCommand(SqlCommandKey sqlCommandKey, out SqlCommandValue sqlCommandValue)
		{
			return this.SqlDatabaseContext.formattedUpdateSqlCache.TryGetValue(sqlCommandKey, out sqlCommandValue);
		}

		public override void Delete(SqlDeleteExpression deleteExpression)
		{
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(deleteExpression, SqlQueryFormatterOptions.Default);
			
			using (var command = this.CreateCommand())
			{
				command.CommandText = formatResult.CommandText;

				foreach (var value in formatResult.ParameterValues)
				{
					this.AddParameter(command, value.Item1, value.Item2);
				}

				Logger.Debug(() => this.FormatCommand(command));

				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception e)
				{
					var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? this.FormatCommand(command);
					var decoratedException = this.SqlDatabaseContext.DecorateException(e, null, relatedSql);

					Logger.ErrorFormat(e.ToString());

					if (decoratedException != e)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}

		public static MethodInfo GetDeleteMethod(Type type)
		{
			return DeleteHelperMethod.MakeGenericMethod(type);
		}

		internal static void DeleteHelper<T>(T type, Expression<Func<T, bool>> condition)
		{
		}

		public override void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
			var parameter = Expression.Parameter(typeDescriptor.Type, "value");

			Expression body = null;

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var currentExpression = Expression.Equal(parameter, Expression.Constant(dataAccessObject));

				if (body == null)
				{
					body = currentExpression;
				}
				else
				{
					body = Expression.OrElse(body, currentExpression);
				}
			}
			
			if (body == null)
			{
				return;
			}

			var condition = Expression.Lambda(body, parameter);
			var expression = (Expression)Expression.Call(null, GetDeleteMethod(typeDescriptor.Type), Expression.Constant(null, typeDescriptor.Type), condition);
			
			expression = Evaluator.PartialEval(expression);
			expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
			expression = SqlObjectOperandComparisonExpander.Expand(expression);
			expression = SqlQueryProvider.Optimize(expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());

			this.Delete((SqlDeleteExpression)expression);
		}
	}
}
