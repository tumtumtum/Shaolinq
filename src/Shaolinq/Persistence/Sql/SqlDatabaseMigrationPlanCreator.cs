// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Shaolinq.Persistence.Sql
{
	public class SqlDatabaseMigrationPlanCreator
		: MigrationPlanCreator
	{
		public SqlDatabaseMigrationPlanCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
		}

		public override DatabaseMigrationPlan CreateMigrationPlan()
		{
			var migrationPlan = new DatabaseMigrationPlan();

			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			{
				using (var dataTransactionContext = this.SystemDataBasedDatabaseConnection.NewDataTransactionContext(this.Model, Transaction.Current))
				{
					if (this.SystemDataBasedDatabaseConnection.SupportsDisabledForeignKeyCheckContext)
					{
						using (this.SystemDataBasedDatabaseConnection.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
						{
							foreach (var typeDescriptor in this.ModelTypeDescriptor.GetQueryableTypeDescriptors(this.Model))
							{
								var tableDescriptor = this.SystemDataBasedDatabaseConnection.GetTableDescriptor(typeDescriptor.GetPersistedName(this.Model));

								if (tableDescriptor == null)
								{
									migrationPlan.NewTypes.Add(new MigrationTypeInfo(this.Model, typeDescriptor));

									continue;
								}
                                
								var migrationTypeInfo = CreateTypeMigrationPlan(migrationPlan, typeDescriptor, tableDescriptor);

								if (migrationTypeInfo != null)
								{
									migrationPlan.ModifiedTypes.Add(migrationTypeInfo);
								}
							}
						}
					}
					else
					{
						throw new NotSupportedException(String.Format("DatabaseConnection '{0}' does not support SupportsDisabledForeignKeyCheckContext", this.SystemDataBasedDatabaseConnection.GetType()));
					}

					scope.Complete();
				}
			}

			return migrationPlan;
		}

		private MigrationTypeInfo CreateTypeMigrationPlan(DatabaseMigrationPlan plan, TypeDescriptor typeDescriptor, TableDescriptor tableDescriptor)
		{
			//Debug.Assert(typeDescriptor != null && typeDescriptor.Type != null && !typeDescriptor.Type.Name.ToLower().EndsWith("ack"));
			
			MigrationTypeInfo retval = (this.Model != null && typeDescriptor.Type != null)? new MigrationTypeInfo(this.Model, typeDescriptor):null;
			
			var existingPropertyNames = new Dictionary<string, PropertyDescriptor>();
			var existingColumnNames = new HashSet<string>(tableDescriptor.Columns.Select(c => c.ColumnName));

			var sqlSchemaWriter = this.SystemDataBasedDatabaseConnection.NewSqlSchemaWriter(this.Model);

			foreach (var property in typeDescriptor.PersistedProperties)
			{
				foreach (var name in sqlSchemaWriter.GetPersistedNames(property))
				{
					existingPropertyNames.Add(name, property);
				}
			}

			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos())
			{
				if (typeRelationshipInfo.EntityRelationshipType == EntityRelationshipType.ChildOfOneToMany)
				{
					var relatedPropertyTypeDescriptor = this.ModelTypeDescriptor.GetQueryableTypeDescriptor(typeRelationshipInfo.RelatedProperty.PropertyType);

					foreach (var primaryKeyProperty in relatedPropertyTypeDescriptor.PrimaryKeyProperties)
					{
						var propName = typeRelationshipInfo.RelatedProperty.PersistedName + primaryKeyProperty.PersistedShortName;
						existingPropertyNames[propName] = typeRelationshipInfo.RelatedProperty;

						if (!existingColumnNames.Contains(propName))
						{
							retval.NewProperties.Add(new MigrationPropertyInfo()
							{
								PropertyDescriptor = typeRelationshipInfo.RelatedProperty,
								PropertyName = propName,
								PersistedName = typeRelationshipInfo.RelatedProperty.PersistedName
							});
						}
					}
				}
			}
		
			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				if (sqlSchemaWriter.GetPersistedNames(propertyDescriptor).Any(existingColumnNames.Contains))
				{
					continue;
				}

				if (retval == null)
				{
					retval = new MigrationTypeInfo(this.Model, typeDescriptor);
				}

				retval.NewProperties.Add(new MigrationPropertyInfo()
				{
					PropertyDescriptor = propertyDescriptor,
				    PropertyName = propertyDescriptor.PropertyName,
				    PersistedName = propertyDescriptor.PersistedName
				});
			}

			var propertiesUpdated = new HashSet<PropertyDescriptor>();

			foreach (var columnDescriptor in tableDescriptor.Columns)
			{
				PropertyDescriptor propertyDescriptor;

				if (existingPropertyNames.TryGetValue(columnDescriptor.ColumnName, out propertyDescriptor))
				{
					MigrationPropertyInfo migrationPropertyInfo = null;

					if (propertyDescriptor.PropertyType == typeof(string) || propertyDescriptor.PropertyType == typeof(Guid) || propertyDescriptor.PropertyType == typeof(Guid?))
					{
						var sqlDataType = this.SystemDataBasedDatabaseConnection.SqlDataTypeProvider.GetSqlDataType(propertyDescriptor.PropertyType);
						var newSize = sqlDataType.GetDataLength(propertyDescriptor);

						var length = (long)columnDescriptor.DataLength;

						if (length < 0)
						{
							length = Int64.MaxValue;
						}
						
						if (length < newSize || columnDescriptor.DataType != sqlDataType.SupportedType)
						{
							if (retval == null)
							{
								retval = new MigrationTypeInfo(this.Model, typeDescriptor);
							}

							migrationPropertyInfo = new MigrationPropertyInfo()
							{
								PersistedName = columnDescriptor.ColumnName ,
								PropertyDescriptor = propertyDescriptor,
								PropertyName = propertyDescriptor.PropertyName,
								CurrentSize = columnDescriptor.DataLength,
								NewSize = newSize,
								OldType = columnDescriptor.DataType
							};

							retval.ModifiedProperties.Add(migrationPropertyInfo);
						}
					}

					continue;
				}

				if (retval == null)
				{
					retval = new MigrationTypeInfo(this.Model, typeDescriptor);
				}

				retval.OldProperties.Add(new MigrationPropertyInfo()
				{
					PersistedName = columnDescriptor.ColumnName
				});
			}

			// Create Index Migration Plan

			CreateIndexMigrationPlan(sqlSchemaWriter, typeDescriptor, tableDescriptor, ref retval);
			
			return retval;
		}

		private bool HasIndex(SqlSchemaWriter sqlSchemaWriter, TypeDescriptor typeDescriptor, TableDescriptor tableDescriptor, IndexDescriptor indexDescriptor)
		{
			foreach (var tableIndex in tableDescriptor.Indexes)
			{
				var found = true;

				if (tableIndex.Name == sqlSchemaWriter.GetQualifiedIndexName(typeDescriptor, indexDescriptor))
				{
					return true;
				}

				if (indexDescriptor.AlsoIndexToLower)
				{
					continue;
				}

				if (tableIndex.Columns.Count == 0)
				{
					continue;
				}
				
				for (var i = 0 ;i < tableIndex.Columns.Count ;i++)
				{
					var columnDescriptor = tableIndex.Columns[i];

					var innerFound = false;

					for (var j = 0 ;j < tableIndex.Columns.Count ;j++)
					{
						if (indexDescriptor.Properties.Count > i)
						{
							continue;
						}

						if (sqlSchemaWriter.GetPersistedNames(indexDescriptor.Properties[i]).Contains(columnDescriptor.ColumnName))
						{
							innerFound = true;

							break;
						}
					}

					if (!innerFound)
					{
						found = false;

						break;
					}
				}

				if (found)
				{
					return true;
				}
			}

			return false;
		}

		private bool HasIndex(SqlSchemaWriter sqlSchemaWriter, TypeDescriptor typeDescriptor, TableIndexDescriptor tableIndexDescriptor)
		{
			// Check if it is the primary key

			if (tableIndexDescriptor.Columns.Count == typeDescriptor.PrimaryKeyCount)
			{
				var found = true;

				foreach (var property in typeDescriptor.PrimaryKeyProperties)
				{
					if (!sqlSchemaWriter.GetPersistedNames(property).Any(c => tableIndexDescriptor.Columns.Any(d => c == d.ColumnName)))
					{
						found = false;

						break;
					}
				}

				if (found)
				{
					return true;
				}
			}

			foreach (var index in typeDescriptor.Indexes)
			{
				var found = true;

				if (sqlSchemaWriter.GetQualifiedIndexName(typeDescriptor, index) == tableIndexDescriptor.Name)
				{
					return true;
				}

				if (index.AlsoIndexToLower)
				{
					continue;
				}

				if (tableIndexDescriptor.Columns.Count == 0)
				{
					continue;
				}

				for (var i = 0; i < index.Properties.Count; i++)
				{
					var propertyDescriptor = index.Properties[i];

					var innerFound = false;

					for (var j = 0; j < tableIndexDescriptor.Columns.Count; j++)
					{
						if (sqlSchemaWriter.GetPersistedNames(propertyDescriptor).Contains(tableIndexDescriptor.Columns[j].ColumnName))
						{
							innerFound = true;

							break;
						}
					}

					if (!innerFound)
					{
						found = false;

						break;
					}
				}

				if (found)
				{
					return true;
				}
			}

			return false;
		}

		private void CreateIndexMigrationPlan(SqlSchemaWriter sqlSchemaWriter, TypeDescriptor typeDescriptor, TableDescriptor tableDescriptor, ref MigrationTypeInfo migrationTypeInfo)
		{
			foreach (var indexDescriptor in typeDescriptor.Indexes)
			{
				if (!HasIndex(sqlSchemaWriter, typeDescriptor, tableDescriptor, indexDescriptor))
				{
					if (migrationTypeInfo == null)
					{
						migrationTypeInfo = new MigrationTypeInfo(this.Model, typeDescriptor);
					}

					migrationTypeInfo.NewIndexes.Add(indexDescriptor);
				}
			}

			foreach (var tableIndexDescriptor in tableDescriptor.Indexes)
			{
				if (!HasIndex(sqlSchemaWriter, typeDescriptor, tableIndexDescriptor))
				{
					if (migrationTypeInfo == null)
					{
						migrationTypeInfo = new MigrationTypeInfo(this.Model, typeDescriptor);
					}

					migrationTypeInfo.OldIndexes.Add(tableIndexDescriptor);
				}
			}
		}
	}
}
