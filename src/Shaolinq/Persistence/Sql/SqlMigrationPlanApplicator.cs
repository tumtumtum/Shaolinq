// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Text;

namespace Shaolinq.Persistence.Sql
{
	public class SqlMigrationPlanApplicator
		: MigrationPlanApplicator
	{
		private readonly SqlSchemaWriter schemaWriter;

		public SqlMigrationPlanApplicator(SqlPersistenceContext sqlPersistenceContext, DataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
			schemaWriter = this.SqlPersistenceContext.NewSqlSchemaWriter(model, persistenceContextInfo);
		}

		protected virtual string CreateMigrationScript(MigrationTypeInfo info)
		{
			var retval = new StringBuilder();

			retval.AppendFormat("/* TABLE: {0} (MODIFIED) */", info.TypeName);
			retval.AppendLine();

			// Drop old columns

			foreach (var property in info.OldProperties)
			{
				WriteDropColumn(retval, info.TypeDescriptor, property.PersistedName);
			}

			// Add new columns

			foreach (var property in info.NewProperties)
			{
				WriteAddColumn(retval, info.TypeDescriptor, property.PropertyDescriptor);
			}
			
			// Modify existing columnns

			foreach (var property in info.ModifiedProperties)
			{
				WriteModifyColumn(retval, info.TypeDescriptor, property);
			}

			retval.AppendLine();

			// Drop old indexes

			foreach (var index in info.OldIndexes)
			{
				WriteDropIndex(retval, info.TypeDescriptor, index);
				retval.AppendLine();
			}
            
			// Create new indexes

			foreach (var index in info.NewIndexes)
			{
				schemaWriter.WriteCreateIndex(retval, info.TypeDescriptor, index);
				retval.AppendLine();
			}
			
			return retval.ToString();
		}

		private void WriteDropIndex(StringBuilder sql, TypeDescriptor typeDescriptor, TableIndexDescriptor index)
		{
			sql.AppendFormat(@"DROP INDEX {0};", index.Name);
		}

		protected virtual void WriteModifyColumn(StringBuilder sql, TypeDescriptor typeDescriptor, MigrationPropertyInfo migrationPropertyInfo)
		{
			if (migrationPropertyInfo.NewSize == migrationPropertyInfo.CurrentSize && migrationPropertyInfo.OldType == migrationPropertyInfo.PropertyDescriptor.PropertyType)
			{
				return;
			}

			var newDataType = this.SqlPersistenceContext.SqlDataTypeProvider.GetSqlDataType(migrationPropertyInfo.PropertyDescriptor.PropertyType).GetMigrationSqlName(migrationPropertyInfo.PropertyDescriptor);

			if (migrationPropertyInfo.PropertyDescriptor.IsRelatedDataAccessObjectsProperty || migrationPropertyInfo.PropertyDescriptor.IsBackReferenceProperty)
			{
				sql.AppendFormat("/* Changing column with FKC */");
			}

			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SqlPersistenceContext.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.AppendFormat(@" ALTER COLUMN {1}{0}{1} TYPE {2}", migrationPropertyInfo.PersistedName, this.SqlPersistenceContext.SqlDialect.NameQuoteChar, newDataType);
			sql.AppendLine(";");
		}

		protected virtual void WriteDropColumn(StringBuilder sql, TypeDescriptor typeDescriptor, string columnName)
		{
			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SqlPersistenceContext.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.AppendFormat(@" DROP COLUMN {1}{0}{1} CASCADE", columnName, this.SqlPersistenceContext.SqlDialect.NameQuoteChar);
			sql.AppendLine(";");
		}

		protected virtual void WriteAddColumn(StringBuilder sql, TypeDescriptor typeDescriptor, PropertyDescriptor propertyDescriptor)
		{
			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SqlPersistenceContext.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.Append(@" ADD COLUMN ");
			this.schemaWriter.WriteColumnDefinition(sql, propertyDescriptor, null, false);
			sql.AppendLine(";");
		}

		public override MigrationScripts CreateScripts(PersistenceContextMigrationPlan persistenceContextMigrationPlan)
		{
			// Create new tables

			var retval = new MigrationScripts();
			var databaseCreationContext = new SqlDatabaseCreator.CreateDatabaseContext();
			var databaseCreator = (SqlDatabaseCreator) this.SqlPersistenceContext.NewPersistenceStoreCreator(this.Model, this.PersistenceContextInfo);
            
			foreach (var migrationTypeInfo in persistenceContextMigrationPlan.NewTypes)
			{
				var builder = new StringBuilder();

				var ss = databaseCreator.GetCreateStrings(databaseCreationContext, migrationTypeInfo.TypeDescriptor);

				builder.AppendFormat("/* TABLE: {0} (NEW) */", migrationTypeInfo.TypeName);
				builder.AppendLine();

				foreach (var s in ss)
				{
					builder.AppendLine(s);
				}
                
				retval.AddScript(this.Model, migrationTypeInfo.TypeDescriptor, builder.ToString());
			}

			// Modify existing tables

			foreach (var migrationTypeInfo in persistenceContextMigrationPlan.ModifiedTypes)
			{
				retval.AddScript(this.Model, migrationTypeInfo.TypeDescriptor, CreateMigrationScript(migrationTypeInfo));
			}

			return retval;
		}
	}
}
