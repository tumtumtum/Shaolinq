// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence
{
	public partial class DefaultSqlTransactionalCommandsContext
	{
		#region ExecuteNonQuery
		[RewriteAsync]
		public override int ExecuteNonQuery(string sql, IReadOnlyList<TypedValue> parameters)
		{
			var command = CreateCommand();

			try
			{
				foreach (var value in parameters)
				{
					AddParameter(command, value.Type, value.Name, value.Value);
				}

				command.CommandText = sql;

				Logger.Info(() => FormatCommand(command));

				try
				{
					return command.ExecuteNonQueryEx(this.DataAccessModel);
				}
				catch (Exception e)
				{
					command?.Dispose();
					command = null;

					var decoratedException = LogAndDecorateException(e, command);

					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
			catch
			{
				command?.Dispose();

				throw;
			}
		}
		#endregion

		#region ExecuteReader
		[RewriteAsync]
		public override ExecuteReaderContext ExecuteReader(string sql, IReadOnlyList<TypedValue> parameters)
		{
			var command = CreateCommand();

			try
			{
				foreach (var value in parameters)
				{
					AddParameter(command, value.Type, value.Name, value.Value);
				}

				command.CommandText = sql;

				Logger.Info(() => FormatCommand(command));

				try
				{
					return new ExecuteReaderContext(command.ExecuteReaderEx(this.DataAccessModel), command);
				}
				catch (Exception e)
				{
					command?.Dispose();
					command = null;

					var decoratedException = LogAndDecorateException(e, command);

					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
			catch
			{
				command?.Dispose();

				throw;
			}
		}
		#endregion

		#region Update

		[RewriteAsync]
		public override void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				if (dataAccessObject.GetAdvanced().IsCommitted)
				{
					continue;
				}

				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				if ((objectState & (DataAccessObjectState.Changed | DataAccessObjectState.ServerSidePropertiesHydrated)) == 0)
				{
					continue;
				}

				if ((objectState & DataAccessObjectState.New) == DataAccessObjectState.New) // already been committed
				{
					continue;
				}

				using (var command = BuildUpdateCommand(typeDescriptor, dataAccessObject))
				{
					if (command == null)
					{
						Logger.ErrorFormat("Object is reported as changed but GetChangedProperties returns an empty list ({0})", dataAccessObject);

						continue;
					}

					Logger.Info(() => FormatCommand(command));

					int result;

					try
					{
						result = command.ExecuteNonQueryEx(this.DataAccessModel);
					}
					catch (Exception e)
					{
						var decoratedException = LogAndDecorateException(e, command);

						if (decoratedException != null)
						{
							throw decoratedException;
						}

						throw;
					}

					if (result == 0)
					{
						throw new MissingDataAccessObjectException(dataAccessObject, null, command.CommandText);
					}
				}

				dataAccessObject.ToObjectInternal().SetIsCommitted();
			}
		}

		#endregion

		#region Insert

		[RewriteAsync]
		public override InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var listToFixup = new List<DataAccessObject>();
			var listToRetry = new List<DataAccessObject>();

			var canDefer = !this.DataAccessModel.hasAnyAutoIncrementValidators;

			foreach (var dataAccessObject in dataAccessObjects)
			{
				if (dataAccessObject.GetAdvanced().IsCommitted)
				{
					continue;
				}

				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				switch (objectState & DataAccessObjectState.NewChanged)
				{
					case DataAccessObjectState.Unchanged:
						continue;
					case DataAccessObjectState.New:
					case DataAccessObjectState.NewChanged:
						break;
					case DataAccessObjectState.Changed:
						throw new NotSupportedException($"Changed state not supported {objectState}");
				}

				var primaryKeyIsComplete = (objectState & DataAccessObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) != DataAccessObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties;
				var constraintsDeferrableOrNotReferencingNewObject =
					(canDefer && this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.Deferrability)) ||
					((objectState & DataAccessObjectState.ReferencesNewObject) == 0);
				var objectReadyToBeCommited = primaryKeyIsComplete && constraintsDeferrableOrNotReferencingNewObject;

				if (objectReadyToBeCommited)
				{
					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

					using (var command = BuildInsertCommand(typeDescriptor, dataAccessObject))
					{
retryInsert:
						Logger.Info(() => FormatCommand(command));

						try
						{
							var reader = command.ExecuteReaderEx(this.DataAccessModel);

							using (reader)
							{
								if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
								{
									var dataAccessObjectInternal = dataAccessObject.ToObjectInternal();

									var result = reader.ReadEx();

									if (result)
									{
										ApplyPropertiesGeneratedOnServerSide(dataAccessObject, reader);
									}

									reader.Close();

									if (!dataAccessObjectInternal.ValidateServerSideGeneratedIds())
									{
										Delete(dataAccessObject.GetType(), new[] { dataAccessObject });

										goto retryInsert;
									}

									dataAccessObjectInternal.MarkServerSidePropertiesAsApplied();

									var updateRequired = dataAccessObjectInternal.ComputeServerGeneratedIdDependentComputedTextProperties();

									if (updateRequired)
									{
										Update(dataAccessObject.GetType(), new[] { dataAccessObject });
									}
								}
							}
						}
						catch (Exception e)
						{
							var decoratedException = LogAndDecorateException(e, command);

							if (decoratedException != null)
							{
								throw decoratedException;
							}

							throw;
						}

						if ((objectState & DataAccessObjectState.ReferencesNewObjectWithServerSideProperties) == DataAccessObjectState.ReferencesNewObjectWithServerSideProperties)
						{
							listToFixup.Add(dataAccessObject);
						}
						else
						{
							dataAccessObject.ToObjectInternal().SetIsCommitted();
						}
					}
				}
				else
				{
					listToRetry.Add(dataAccessObject);
				}
			}

			return new InsertResults(listToFixup, listToRetry);
		}
		#endregion

		#region DeleteExpression

		[RewriteAsync]
		public override void Delete(SqlDeleteExpression deleteExpression)
		{
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(deleteExpression, SqlQueryFormatterOptions.Default);

			using (var command = CreateCommand())
			{
				command.CommandText = formatResult.CommandText;

				foreach (var value in formatResult.ParameterValues)
				{
					AddParameter(command, value.Type, value.Name, value.Value);
				}

				Logger.Info(() => FormatCommand(command));

				try
				{
					var count = command.ExecuteNonQueryEx(this.DataAccessModel);
				}
				catch (Exception e)
				{
					var decoratedException = LogAndDecorateException(e, command);

					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}

		#endregion

		#region DeleteObjects

		[RewriteAsync]
		public override void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var provider = new SqlQueryProvider(this.DataAccessModel, this.SqlDatabaseContext);
			var expression = BuildDeleteExpression(type, dataAccessObjects);

			if (expression == null)
			{
				return;
			}

			((ISqlQueryProvider)provider).Execute<int>(expression);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				dataAccessObject.ToObjectInternal().SetIsCommitted();
			}
		}

		public virtual Expression BuildDeleteExpression(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
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
				return null;
			}
			
			var condition = Expression.Lambda(body, parameter);
			var expression = (Expression)Expression.Call(Expression.Constant(this.DataAccessModel), MethodInfoFastRef.DataAccessModelGetDataAccessObjectsMethod.MakeGenericMethod(typeDescriptor.Type));

			expression = Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeDescriptor.Type), expression, Expression.Quote(condition));
			expression = Expression.Call(MethodInfoFastRef.QueryableExtensionsDeleteMethod.MakeGenericMethod(typeDescriptor.Type), expression);

			return expression;
		}

		#endregion
	}
}
