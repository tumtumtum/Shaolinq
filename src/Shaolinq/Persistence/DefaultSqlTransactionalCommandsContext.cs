// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using log4net;
using Platform; 

namespace Shaolinq.Persistence
{
	public class DefaultSqlTransactionalCommandsContext
		: SqlTransactionalCommandsContext
	{
		protected int disposed = 0;
		public static readonly ILog Logger = LogManager.GetLogger(typeof(Sql92QueryFormatter));

		protected internal struct SqlCommandValue
		{
			public string commandText;
		}

		protected internal struct SqlCommandKey
		{
			public readonly Type dataAccessObjectType;
			public readonly List<PropertyInfoAndValue> changedProperties;

			public SqlCommandKey(Type dataAccessObjectType, List<PropertyInfoAndValue> changedProperties)
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
					if (!Object.ReferenceEquals(x.changedProperties[i].persistedName, y.changedProperties[i].persistedName))
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
					retval ^= obj.changedProperties[0].propertyNameHashCode;

					if (count > 1)
					{
						retval ^= obj.changedProperties[count - 1].propertyNameHashCode;
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

		private static DbType GetDbType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;

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
					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
					{
						return DbType.AnsiString;
					}
					else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
					{
						return DbType.AnsiString;
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

		public virtual IDataReader ExecuteReader(string sql, IEnumerable<Pair<Type, object>> parameters)
		{
			var x = 0;
			var command = CreateCommand();
            
			foreach (var value in parameters)
			{
				var parameter = command.CreateParameter();

				parameter.ParameterName = this.parameterIndicatorPrefix + "param" + x;
				parameter.DbType = GetDbType(value.Left);
				parameter.Value = value.Right;

				command.Parameters.Add(parameter);

				x++;
			}
            
			command.CommandText = sql;

			if (Logger.IsDebugEnabled)
			{
				Logger.Debug(FormatCommand(command));
			}

			try
			{
				return command.ExecuteReader();
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? FormatCommand(command);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, relatedSql);

				Logger.ErrorFormat(e.ToString());

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}
		}

		internal string FormatCommand(IDbCommand command)
		{
			return this.SqlDatabaseContext.SqlQueryFormatterManager.Format(command.CommandText, c =>
			{
				if (!command.Parameters.Contains(c))
				{
					return "[FormatCommandError!]";
				}

				return ((IDbDataParameter)command.Parameters[c]);
			});
		}

		public virtual object ExecuteScalar(string sql, IEnumerable<Pair<Type, object>> parameters)
		{
			var command = CreateCommand();

			command.CommandText = sql;

			foreach (var parameter in parameters)
			{
				command.Parameters.Add(parameter.Right);
			}
			
			if (Logger.IsDebugEnabled)
			{
				Logger.Debug(FormatCommand(command));
			}

			try
			{
				return command.ExecuteScalar();
			}
			catch (Exception e)
			{
				var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? FormatCommand(command);
				var decoratedException = this.SqlDatabaseContext.DecorateException(e, relatedSql);

				Logger.ErrorFormat(e.ToString());

				if (decoratedException != e)
				{
					throw decoratedException;
				}

				throw;
			}
		}

		public override int Update(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				if (dataAccessObject.HasObjectChanged || (dataAccessObject.ObjectState & (ObjectState.Changed | ObjectState.MissingForeignKeys | ObjectState.MissingUnconstrainedForeignKeys)) != 0)
				{
					var command = this.BuildUpdateCommand(typeDescriptor, dataAccessObject);

					if (command == null)
					{
						if (Logger.IsDebugEnabled)
						{
							Logger.ErrorFormat("Object {0} is reported as changed but GetChangedProperties returns an empty list", dataAccessObject.ToString());
						}

						continue;
					}

					if (Logger.IsDebugEnabled)
					{
						Logger.Debug(FormatCommand(command));
					}

					int retval;

					try
					{
						retval = command.ExecuteNonQuery();
					}
					catch (Exception e)
					{
						var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? FormatCommand(command);
						var decoratedException = this.SqlDatabaseContext.DecorateException(e, relatedSql);

						Logger.ErrorFormat(e.ToString());

						if (decoratedException != e)
						{
							throw decoratedException;
						}

						throw;
					}

					if (retval == 0)
					{
						throw new MissingDataAccessObjectException();
					}

					dataAccessObject.ResetModified();

					return retval;
				}
			}

			return 0;
		}

		public override InsertResults Insert(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
		{
			var listToFixup = new List<IDataAccessObject>();
			var listToRetry = new List<IDataAccessObject>();
			
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.ObjectState;
                
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

				if ((objectState & ObjectState.MissingForeignKeys) == 0)
				{
					// We can insert if it is not missing foreign keys
					// (could be missing some unconstrained foreign keys)

					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

					// BuildInsertCommand will call dataAccessObject.GetChangedProperties()
					// which will include automatically update any foreign key properties
					// that need to be set on this object

					var command = BuildInsertCommand(typeDescriptor, dataAccessObject);

					if (Logger.IsDebugEnabled)
					{
						Logger.Debug(FormatCommand(command));
					}
					
					object result;

					try
					{
						result = command.ExecuteScalar();
					}
					catch (Exception e)
					{
						var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? FormatCommand(command);
						var decoratedException = this.SqlDatabaseContext.DecorateException(e, relatedSql);

						Logger.ErrorFormat(e.ToString());

						if (decoratedException != e)
						{
							throw decoratedException;
						}

						throw;
					}

					// TODO: Don't bother loading auto increment keys if this is an end of transaction flush and we're not needed as foriegn keys
					
					if (dataAccessObject.DefinesAnyAutoIncrementIntegerProperties)
					{
						var propertyInfos = dataAccessObject.GetIntegerAutoIncrementPropertyInfos();

						Debug.Assert(dataAccessObject.NumberOfIntegerAutoIncrementPrimaryKeys == 1);
						
						if (result != null && result.GetType() != propertyInfos[0].PropertyType)
						{
							result = Convert.ChangeType(result, propertyInfos[0].PropertyType);
						}

						dataAccessObject.SetAutoIncrementKeyValue(result);

						if (dataAccessObject.ComputeServerGeneratedIdDependentComputedTextProperties())
						{
							Update(type, new [] { dataAccessObject });
						}
					}

					dataAccessObject.ResetModified();
					dataAccessObject.SetIsNew(false);
				}
				else
				{
					listToRetry.Add(dataAccessObject);
 				}
				
				if ((objectState & ObjectState.MissingUnconstrainedForeignKeys) != 0)
				{
					// Add to fix-ups if it is missing some unconstrained foreign keys
					listToFixup.Add(dataAccessObject);
				}
			}

			return new InsertResults(listToFixup, listToRetry);
		}
        
		private IDbDataParameter AddParameter(IDbCommand command, Type type, object value, bool convertForSql)
		{
			var parameter = command.CreateParameter();

			parameter.ParameterName = this.parameterIndicatorPrefix + "param" + command.Parameters.Count;

			if (value == null)
			{
				parameter.DbType = GetDbType(type);
			}

			if (convertForSql)
			{
				var result = sqlDataTypeProvider.GetSqlDataType(type).ConvertForSql(value);

				parameter.DbType = GetDbType(result.Left);
				parameter.Value = result.Right;
			}
			else
			{
				parameter.Value = value;
			}

			command.Parameters.Add(parameter);

			return parameter;
		}

		private void FillParameters(IDbCommand command, List<PropertyInfoAndValue> changedProperties, PropertyInfoAndValue[] primaryKeys)
		{
			foreach (var infoAndValue in changedProperties)
			{
				AddParameter(command, infoAndValue.propertyInfo.PropertyType, infoAndValue.value, true);
			}

			if (primaryKeys != null)
			{
				foreach (var infoAndValue in primaryKeys)
				{
					AddParameter(command, infoAndValue.propertyInfo.PropertyType, infoAndValue.value, true);
				}
			}
		}

		protected virtual IDbCommand BuildUpdateCommand(TypeDescriptor typeDescriptor, IDataAccessObject dataAccessObject)
		{
			IDbCommand command;
			SqlCommandValue sqlCommandValue;
			var updatedProperties = dataAccessObject.GetChangedProperties();
			
			if (updatedProperties.Count == 0)
			{
				return null;
			}

			var primaryKeys = dataAccessObject.GetPrimaryKeys();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetUpdateCommand(commandKey, out sqlCommandValue))
			{
				command = CreateCommand();
				command.CommandText = sqlCommandValue.commandText;
				FillParameters(command, updatedProperties, primaryKeys);

				return command;
			}
			
			var assignments = new ReadOnlyCollection<Expression>(updatedProperties.Select(c => (Expression)new SqlAssignExpression(new SqlColumnExpression(c.propertyInfo.PropertyType, null, c.persistedName), Expression.Constant(c.value))).ToList());

			Expression where = null;

			var i = 0;

			Debug.Assert(primaryKeys.Length > 0);

			foreach (var primaryKey in primaryKeys)
			{
				var currentExpression = Expression.Equal(new SqlColumnExpression(primaryKey.propertyInfo.PropertyType, null, primaryKey.persistedName), Expression.Constant(primaryKey.value));

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

			var expression = new SqlUpdateExpression(SqlQueryFormatter.PrefixedTableName(tableNamePrefix, typeDescriptor.PersistedName), assignments, where);
			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default & ~SqlQueryFormatterOptions.OptimiseOutConstantNulls);

			command = CreateCommand();

			command.CommandText = result.CommandText;
			CacheUpdateCommand(commandKey, new SqlCommandValue() { commandText = command.CommandText }); 
			FillParameters(command, updatedProperties, primaryKeys);
			
			return command;
		}

		protected virtual IDbCommand BuildInsertCommand(TypeDescriptor typeDescriptor, IDataAccessObject dataAccessObject)
		{
			IDbCommand command;
			SqlCommandValue sqlCommandValue;
			
			var updatedProperties = dataAccessObject.GetChangedProperties();
			var commandKey = new SqlCommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.TryGetInsertCommand(commandKey, out sqlCommandValue))
			{
				command = CreateCommand();
				command.CommandText = sqlCommandValue.commandText;
				FillParameters(command, updatedProperties, null);

				return command;
			}

			string returningAutoIncrementColumnName = null;

			if (dataAccessObject.DefinesAnyAutoIncrementIntegerProperties)
			{
				var propertyDescriptors = typeDescriptor.PrimaryKeyProperties.Where(c => c.IsAutoIncrement && c.IsPropertyThatIsCreatedOnTheServerSide).ToList();

				Debug.Assert(propertyDescriptors.Count == 1);

				returningAutoIncrementColumnName = propertyDescriptors[0].PersistedName;
			}

			var columnNames = new ReadOnlyCollection<string>(updatedProperties.Select(c => c.persistedName).ToList());
			var valueExpressions = new ReadOnlyCollection<Expression>(updatedProperties.Select(c => (Expression)Expression.Constant(c.value)).ToList());
			var expression = new SqlInsertIntoExpression(SqlQueryFormatter.PrefixedTableName(tableNamePrefix, typeDescriptor.PersistedName), columnNames, returningAutoIncrementColumnName, valueExpressions);

			var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(expression, SqlQueryFormatterOptions.Default & ~SqlQueryFormatterOptions.OptimiseOutConstantNulls);

			Debug.Assert(result.ParameterValues.Count() == updatedProperties.Count);

			command = CreateCommand();

			command.CommandText = result.CommandText;
			CacheInsertCommand(commandKey, new SqlCommandValue { commandText = command.CommandText });
			FillParameters(command, updatedProperties, null);

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
			
			using (var command = CreateCommand())
			{
				command.CommandText = formatResult.CommandText;

				foreach (var value in formatResult.ParameterValues)
				{
					this.AddParameter(command, value.Left, value.Right, false);
				}

				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception e)
				{
					var relatedSql = this.SqlDatabaseContext.GetRelatedSql(e) ?? FormatCommand(command);
					var decoratedException = this.SqlDatabaseContext.DecorateException(e, relatedSql);

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

		public override void Delete(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
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
			
			expression = Evaluator.PartialEval(this.DataAccessModel, expression);
			expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
			expression = ObjectOperandComparisonExpander.Expand(expression);
			expression = SqlQueryProvider.Optimize(this.DataAccessModel, expression);

			Delete((SqlDeleteExpression)expression);
		}
	}
}
