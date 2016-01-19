using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.Persistence
{
	public partial class DefaultSqlTransactionalCommandsContext
	{
		private static readonly MethodInfo DeleteHelperMethod = TypeUtils.GetMethod(() => DeleteHelper(0, null)).GetGenericMethodDefinition();

		#region ExecuteReader
		public override IDataReader ExecuteReader(string sql, IReadOnlyList<Tuple<Type, object>> parameters)
		{
			return this.ExecuteReaderAsyncHelper(sql, parameters, false, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task<IDataReader> ExecuteReaderAsync(string sql, IReadOnlyList<Tuple<Type, object>> parameters, CancellationToken cancellationToken)
		{
			return this.ExecuteReaderAsyncHelper(sql, parameters, true, cancellationToken);
		}

		private async Task<IDataReader> ExecuteReaderAsyncHelper(string sql, IReadOnlyList<Tuple<Type, object>> parameters, bool async, CancellationToken cancellationToken)
		{
			using (var command = this.CreateCommand())
			{
				foreach (var value in parameters)
				{
					this.AddParameter(command, value.Item1, value.Item2);
				}

				command.CommandText = sql;

				Logger.Debug(() => this.FormatCommand(command));

				try
				{
					if (async)
					{
						return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						return command.ExecuteReader();
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
			}
		}
		#endregion

		#region Update
		public override void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			this.UpdateAsyncHelper(type, dataAccessObjects, false, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task UpdateAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			return UpdateAsyncHelper(type, dataAccessObjects, true, cancellationToken);
		}

        private async Task UpdateAsyncHelper(Type type, IEnumerable<DataAccessObject> dataAccessObjects, bool async, CancellationToken cancellationToken)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				if ((objectState & (ObjectState.Changed | ObjectState.ServerSidePropertiesHydrated)) == 0)
				{
					continue;
				}

				using (var command = this.BuildUpdateCommand(typeDescriptor, dataAccessObject))
				{

					if (command == null)
					{
						Logger.ErrorFormat("Object is reported as changed but GetChangedProperties returns an empty list ({0})", dataAccessObject);

						continue;
					}

					Logger.Debug(() => this.FormatCommand(command));

					int result;

					try
					{
						if (async)
						{
							result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
						}
						else
						{
							result = command.ExecuteNonQuery();
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

					if (result == 0)
					{
						throw new MissingDataAccessObjectException(dataAccessObject, null, command.CommandText);
					}

					dataAccessObject.ToObjectInternal().ResetModified();
				}
			}
		}
		#endregion

		#region Insert

		public override InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			return this.InsertAsyncHelper(type, dataAccessObjects, false, CancellationToken.None).GetAwaiter().GetResult();
		}

        public override Task<InsertResults> InsertAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			return this.InsertAsyncHelper(type, dataAccessObjects, true, cancellationToken);
		}

        private async Task<InsertResults> InsertAsyncHelper(Type type, IEnumerable<DataAccessObject> dataAccessObjects, bool async, CancellationToken cancellationToken)
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

				var primaryKeyIsComplete = (objectState & ObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) == 0;
				var deferrableOrNotReferencingNewObject = (this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.Deferrability) || ((objectState & ObjectState.ReferencesNewObject) == 0));

				var objectReadyToBeCommited = primaryKeyIsComplete && deferrableOrNotReferencingNewObject;

				if (objectReadyToBeCommited)
				{
					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

					using (var command = this.BuildInsertCommand(typeDescriptor, dataAccessObject))
					{
						Logger.Debug(() => this.FormatCommand(command));

						try
						{
							IDataReader reader;

							if (async)
							{
								reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
							}
							else
							{
								reader = command.ExecuteReader();
							}

							using (reader)
							{
								if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
								{
									bool result;
									var dataAccessObjectInternal = dataAccessObject.ToObjectInternal();

									if (async)
									{
										result = await reader.ReadAsync(cancellationToken);
									}
									else
									{
										result = reader.Read();
									}

									if (result)
									{
										this.ApplyPropertiesGeneratedOnServerSide(dataAccessObject, reader);
										dataAccessObjectInternal.MarkServerSidePropertiesAsApplied();
									}

									reader.Close();

									if (dataAccessObjectInternal.ComputeServerGeneratedIdDependentComputedTextProperties())
									{
										this.Update(dataAccessObject.GetType(), new[] { dataAccessObject });
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

						if ((objectState & ObjectState.ReferencesNewObjectWithServerSideProperties) == ObjectState.ReferencesNewObjectWithServerSideProperties)
						{
							listToFixup.Add(dataAccessObject);
						}
						else
						{
							dataAccessObject.ToObjectInternal().ResetModified();
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

		public override void Delete(SqlDeleteExpression deleteExpression)
		{
			this.DeleteAsyncHelper(deleteExpression, false, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task DeleteAsync(SqlDeleteExpression deleteExpression, CancellationToken cancellationToken)
		{
			return this.DeleteAsyncHelper(deleteExpression, true, cancellationToken);
		}

        private async Task DeleteAsyncHelper(SqlDeleteExpression deleteExpression, bool async, CancellationToken cancellationToken)
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
					if (async)
					{
						await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						command.ExecuteNonQuery();
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
			}
		}

		#endregion

		#region DeleteObjects

		public override void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			this.DeleteAsyncHelper(type, dataAccessObjects, false, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task DeleteAsync(Type type, IEnumerable<DataAccessObject> dataAccessObjects, CancellationToken cancellationToken)
		{
			return this.DeleteAsyncHelper(type, dataAccessObjects, true, cancellationToken);
		}

        private async Task DeleteAsyncHelper(Type type, IEnumerable<DataAccessObject> dataAccessObjects, bool async, CancellationToken cancellationToken)
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
			var expression = (Expression)Expression.Call(GetDeleteMethod(typeDescriptor.Type), Expression.Constant(null, typeDescriptor.Type), condition);

			expression = Evaluator.PartialEval(expression);
			expression = QueryBinder.Bind(this.DataAccessModel, expression, null, null);
			expression = SqlObjectOperandComparisonExpander.Expand(expression);
			expression = SqlQueryProvider.Optimize(this.DataAccessModel, expression, this.SqlDatabaseContext.SqlDataTypeProvider.GetTypeForEnums());

	        if (async)
	        {
		        await this.DeleteAsync((SqlDeleteExpression)expression, cancellationToken).ConfigureAwait(false);
			}
	        else
	        {
		        this.Delete((SqlDeleteExpression)expression);
	        }
		}

		#endregion
	}
}
