// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using Shaolinq.Persistence.Sql.Linq;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Optimizer;
using log4net;
using Platform; 

namespace Shaolinq.Persistence.Sql
{
	public abstract class SqlPersistenceTransactionContext
		: PersistenceTransactionContext
	{
		protected int disposed = 0;
		public static readonly ILog Logger = LogManager.GetLogger(typeof(Sql92QueryFormatter));
		
		protected struct CommandValue
		{
			public string commandText;
		}

		public override bool IsClosed
		{
			get
			{
				return this.DbConnection.State == ConnectionState.Closed || this.DbConnection.State == ConnectionState.Broken;
			}
		}

		protected abstract bool IsDataAccessException(Exception e);
		protected abstract bool IsConcurrencyException(Exception e);

		protected virtual string GetRelatedSql(Exception e)
		{
			return string.Empty;
		}

		protected class CommandKeyComparer
			: IEqualityComparer<CommandKey>
		{
			public static readonly CommandKeyComparer Default = new CommandKeyComparer();

			public bool Equals(CommandKey x, CommandKey y)
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

			public int GetHashCode(CommandKey obj)
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

		protected struct CommandKey
		{
			public readonly Type dataAccessObjectType;
			public readonly List<PropertyInfoAndValue> changedProperties;

			public CommandKey(Type dataAccessObjectType, List<PropertyInfoAndValue> changedProperties)
			{
				this.dataAccessObjectType = dataAccessObjectType;
				this.changedProperties = changedProperties;
			}
		}

		public IDbConnection DbConnection
		{
			get; set;
		}

		public DataAccessModel DataAccessModel
		{
			get;
			private set;
		}

		public SqlPersistenceContext PersistenceContext
		{
			get;
			private set;
		}

		private readonly SqlDataTypeProvider sqlDataTypeProvider;

		protected SqlPersistenceTransactionContext(SqlPersistenceContext persistenceContext, DataAccessModel dataAccessModel, Transaction transaction)
		{
			this.PersistenceContext = persistenceContext;
			sqlDataTypeProvider = persistenceContext.SqlDataTypeProvider;

			this.DbConnection = persistenceContext.OpenConnection();

			this.DataAccessModel = dataAccessModel;
		}

		~SqlPersistenceTransactionContext()
		{
			Dispose();
		}

		public override void Dispose()
		{
			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				if (this.DbConnection != null)
				{
					this.DbConnection.Close();
				}

				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Returns the character used to indicate a parameter within an
		/// sql string.  Examples include '@' for sql server and '?' for mysql.
		/// </summary>
		protected abstract char ParameterIndicatorChar
		{
			get;
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

		protected virtual IDbCommand CreateCommand()
		{
			var retval = this.DbConnection.CreateCommand();

			retval.CommandTimeout = (int)this.PersistenceContext.CommandTimeout.TotalSeconds;

			return retval;
		}

		public virtual IDataReader ExecuteReader(string sql, IEnumerable<Pair<Type, object>> parameters)
		{
			var x = 0;
			var command = CreateCommand();
            
			foreach (var value in parameters)
			{
				var parameter = command.CreateParameter();

				parameter.ParameterName = this.ParameterIndicatorChar + "param" + x;
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
				Logger.ErrorFormat(e.ToString());

				if (IsConcurrencyException(e))
				{
					throw new ConcurrencyException(e, GetRelatedSql(e));
				}
				else if (IsDataAccessException(e))
				{
					throw new DataAccessException(e, GetRelatedSql(e));
				}

				throw;
			}
		}

		private static readonly Regex FormatCommandRegex = new Regex(@"[@\?\$\%\#\!]param[0-9]+", RegexOptions.Compiled);

		internal static string FormatCommand(IDbCommand command)
		{
			return FormatCommandRegex.Replace(command.CommandText, match => 
			{
				var value = ((IDbDataParameter)command.Parameters[match.Value]).Value;

				if (value is string)
				{
					return "'" + value + "'";
				}
				else
				{
					return Convert.ToString(value);		
				}
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
				Logger.ErrorFormat(e.ToString());

				if (IsConcurrencyException(e))
				{
					throw new ConcurrencyException(e, GetRelatedSql(e));
				}
				else if (IsDataAccessException(e))
				{
					throw new DataAccessException(e, GetRelatedSql(e));
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
						Logger.ErrorFormat(e.ToString());

						if (IsConcurrencyException(e))
						{
							throw new ConcurrencyException(e, GetRelatedSql(e));
						}
						else if (IsDataAccessException(e))
						{
							throw new DataAccessException(e, GetRelatedSql(e));
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

					try
					{
						command.ExecuteScalar();
					}
					catch (Exception e)
					{
						Logger.ErrorFormat(e.ToString());

						if (IsConcurrencyException(e))
						{
							throw new ConcurrencyException(e, GetRelatedSql(e));
						}
						else if (IsDataAccessException(e))
						{
							throw new DataAccessException(e, GetRelatedSql(e));
						}

						throw;
					}

					// TODO: Don't bother loading auto increment keys if this is an end of transaction flush and we're not needed as foriegn keys
					
					if (dataAccessObject.DefinesAnyAutoIncrementIntegerProperties)
					{
						bool isSingularPrimaryKeyValue = false;
						var propertyInfos = dataAccessObject.GetIntegerAutoIncrementPropertyInfos();

						if (propertyInfos.Length == 1 && dataAccessObject.NumberOfIntegerAutoIncrementPrimaryKeys == 1)
						{
							isSingularPrimaryKeyValue = true;
						}

						var i = 0;
						var values = new object[propertyInfos.Length];

						foreach (var propertyInfo in propertyInfos)
						{
							var tableName = typeDescriptor.GetPersistedName(dataAccessObject.DataAccessModel);
							var columnName = typeDescriptor.GetPropertyDescriptorByPropertyName(propertyInfo.Name).PersistedName;

							var propertyType = propertyInfo.PropertyType.NonNullableType();
							var value = this.GetLastInsertedAutoIncrementValue(tableName, columnName, isSingularPrimaryKeyValue);

							if (value == null)
							{
								i++;
							}
							else if (value.GetType() == propertyType)
							{
								values[i++] = value;
							}
							else
							{
								values[i++] = Convert.ChangeType(value, propertyType);
							}
						}

						dataAccessObject.SetIntegerAutoIncrementValues(values);

						if (dataAccessObject.ComputeServerGeneratedIdDependentComputedTextProperties())
						{
							Update(type, new IDataAccessObject[] { dataAccessObject });
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

		protected abstract object GetLastInsertedAutoIncrementValue(string tableName, string columnName, bool isSingularPrimaryKeyValue); 

		protected void AppendParameter(IDbCommand command, StringBuilder commandText, Type type, object value)
		{
			commandText.Append(this.ParameterIndicatorChar).Append("param").Append(command.Parameters.Count);

			AddParameter(command, type, value, true);
		}
        
		private IDbDataParameter AddParameter(IDbCommand command, Type type, object value, bool convertForSql)
		{
			var parameter = command.CreateParameter();

			parameter.ParameterName = this.ParameterIndicatorChar + "param" + command.Parameters.Count;

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
			CommandValue commandValue;
			var updatedProperties = dataAccessObject.GetChangedProperties();
			var primaryKeys = dataAccessObject.GetPrimaryKeys();

			if (updatedProperties.Count == 0)
			{
				return null;
			}

			var commandKey = new CommandKey(dataAccessObject.GetType(), updatedProperties);

			if (this.UpdateCache.TryGetValue(commandKey, out commandValue))
			{
				command = CreateCommand();
				command.CommandText = commandValue.commandText;
				FillParameters(command, updatedProperties, primaryKeys);

				return command;
			}

			var commandText = new StringBuilder(256 + (updatedProperties.Count * 32));

			command = CreateCommand();

			commandText.Append("UPDATE ").Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
			commandText.Append(typeDescriptor.GetPersistedName(this.DataAccessModel)).Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
			commandText.Append(" SET ");

			for (var i = 0; i < updatedProperties.Count; i++)
			{
				commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
				commandText.Append(updatedProperties[i].persistedName);
				commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
				commandText.Append('=');
				AppendParameter(command, commandText, updatedProperties[i].propertyInfo.PropertyType, updatedProperties[i].value);

				if (i != updatedProperties.Count - 1)
				{
					commandText.Append(",");
				}
			}

			commandText.Append(" WHERE ");

			var j = updatedProperties.Count;
			
			for (var k = 0; k < primaryKeys.Length; k++)
			{
				commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
				commandText.Append(primaryKeys[k].persistedName);
				commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
				commandText.Append('=');
				AppendParameter(command, commandText, primaryKeys[k].propertyInfo.PropertyType, primaryKeys[k].value);

				if (k != primaryKeys.Length - 1)
				{
					commandText.Append(" AND ");
				}
			}

			command.CommandText = commandText.ToString();

			CacheUpdateCommand(commandKey, new CommandValue() { commandText = command.CommandText });

			return command;
		}

		protected virtual IDbCommand BuildInsertCommand(TypeDescriptor typeDescriptor, IDataAccessObject dataAccessObject)
		{
			IDbCommand command;
			CommandValue commandValue;
			var updatedProperties = dataAccessObject.GetChangedProperties();

			if (updatedProperties.Count == 0)
			{
				command = CreateCommand();
				command.CommandText = "INSERT INTO " + this.PersistenceContext.SqlDialect.NameQuoteChar + typeDescriptor.GetPersistedName(this.DataAccessModel) + this.PersistenceContext.SqlDialect.NameQuoteChar + " DEFAULT VALUES";

				return command;
			}

			var commandKey = new CommandKey(dataAccessObject.GetType(), updatedProperties);
            
			if (this.InsertCache.TryGetValue(commandKey, out commandValue))
			{
				command = CreateCommand();
				command.CommandText = commandValue.commandText;
				FillParameters(command, updatedProperties, null);

				return command;
			}

			var commandText = new StringBuilder();

			command = CreateCommand();

			commandText.Append("INSERT INTO ").Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
			commandText.Append(typeDescriptor.GetPersistedName(this.DataAccessModel));
			commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);

			if (updatedProperties.Count > 0 || this.InsertDefaultString == null)
			{
				commandText.Append('(');

				for (int i = 0, lastindex = updatedProperties.Count - 1; i <= lastindex; i++)
				{
					commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);
					commandText.Append(updatedProperties[i].persistedName);
					commandText.Append(this.PersistenceContext.SqlDialect.NameQuoteChar);

					if (i != lastindex)
					{
						commandText.Append(",");
					}
				}

				commandText.Append(')');

				commandText.Append(" VALUES ");

				commandText.Append('(');

				for (int i = 0, lastindex = updatedProperties.Count - 1; i <= lastindex; i++)
				{
					var updatedProperty = updatedProperties[i];

					AppendParameter(command, commandText, updatedProperty.propertyInfo.PropertyType, updatedProperty.value);
					
					if (i != lastindex)
					{
						commandText.Append(",");
					}
				}
				commandText.AppendLine(")");
			}
			else
			{
				commandText.Append(' ').Append(this.InsertDefaultString);
			}

			command.CommandText = commandText.ToString();

			CacheInsertCommand(commandKey, new CommandValue() { commandText = command.CommandText });

			return command;
		}

		protected abstract Dictionary<CommandKey, CommandValue> InsertCache { get; set; }
		protected abstract Dictionary<CommandKey, CommandValue> UpdateCache { get; set; }

		protected virtual void CacheInsertCommand(CommandKey commandKey, CommandValue commandValue)
		{
			var newDictionary = new Dictionary<CommandKey, CommandValue>(this.InsertCache, CommandKeyComparer.Default);
			newDictionary[commandKey] = commandValue;
			this.InsertCache = newDictionary;
		}

		protected virtual bool TryGetInsertCommand(CommandKey commandKey, out CommandValue commandValue)
		{
			return this.InsertCache.TryGetValue(commandKey, out commandValue);
		}

		protected virtual void CacheUpdateCommand(CommandKey commandKey, CommandValue commandValue)
		{
			var newDictionary = new Dictionary<CommandKey, CommandValue>(this.UpdateCache, CommandKeyComparer.Default);
			newDictionary[commandKey] = commandValue;
			this.UpdateCache = newDictionary;
		}

		protected bool TryGetUpdateCommand(CommandKey commandKey, out CommandValue commandValue)
		{
			return this.UpdateCache.TryGetValue(commandKey, out commandValue);
		}

		protected abstract string InsertDefaultString
		{
			get;
		}
		
		public override void Delete(SqlDeleteExpression deleteExpression)
		{
			var formatter = this.PersistenceContext.NewQueryFormatter(this.DataAccessModel, this.PersistenceContext.SqlDataTypeProvider, this.PersistenceContext.SqlDialect, deleteExpression, SqlQueryFormatterOptions.Default);
			var formatResult = formatter.Format();

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
					Logger.ErrorFormat(e.ToString());

					if (IsConcurrencyException(e))
					{
						throw new ConcurrencyException(e, GetRelatedSql(e));
					}
					else if (IsDataAccessException(e))
					{
						throw new DataAccessException(e, GetRelatedSql(e));
					}

					throw;
				}
			}
		}

		internal static readonly MethodInfo DeleteHelperMethod = typeof(SqlPersistenceTransactionContext).GetMethod("DeleteHelper", BindingFlags.Static | BindingFlags.NonPublic);
		
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
