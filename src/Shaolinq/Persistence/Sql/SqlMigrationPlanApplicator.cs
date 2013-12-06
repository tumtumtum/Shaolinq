// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Text;

namespace Shaolinq.Persistence.Sql
{
	public class SqlMigrationPlanApplicator
		: MigrationPlanApplicator
	{
		private readonly SqlSchemaWriter schemaWriter;

		public SqlMigrationPlanApplicator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
			: base(databaseConnection, model)
		{
			schemaWriter = this.SystemDataBasedDatabaseConnection.NewSqlSchemaWriter(model);
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

			var newDataType = this.SystemDataBasedDatabaseConnection.SqlDataTypeProvider.GetSqlDataType(migrationPropertyInfo.PropertyDescriptor.PropertyType).GetMigrationSqlName(migrationPropertyInfo.PropertyDescriptor);

			if (migrationPropertyInfo.PropertyDescriptor.IsRelatedDataAccessObjectsProperty || migrationPropertyInfo.PropertyDescriptor.IsBackReferenceProperty)
			{
				sql.AppendFormat("/* Changing column with FKC */");
			}

			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SystemDataBasedDatabaseConnection.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.AppendFormat(@" ALTER COLUMN {1}{0}{1} TYPE {2}", migrationPropertyInfo.PersistedName, this.SystemDataBasedDatabaseConnection.SqlDialect.NameQuoteChar, newDataType);
			sql.AppendLine(";");
		}

		protected virtual void WriteDropColumn(StringBuilder sql, TypeDescriptor typeDescriptor, string columnName)
		{
			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SystemDataBasedDatabaseConnection.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.AppendFormat(@" DROP COLUMN {1}{0}{1} CASCADE", columnName, this.SystemDataBasedDatabaseConnection.SqlDialect.NameQuoteChar);
			sql.AppendLine(";");
		}

		protected virtual void WriteAddColumn(StringBuilder sql, TypeDescriptor typeDescriptor, PropertyDescriptor propertyDescriptor)
		{
			sql.AppendFormat(@"ALTER TABLE {0}{1}{0}", this.SystemDataBasedDatabaseConnection.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			sql.Append(@" ADD COLUMN ");
			this.schemaWriter.WriteColumnDefinition(sql, propertyDescriptor, null, false);
			sql.AppendLine(";");
		}

		public override MigrationScripts CreateScripts(DatabaseMigrationPlan databaseMigrationPlan)
		{
			// Create new tables

			var retval = new MigrationScripts();
			var databaseCreationContext = new SqlDatabaseCreator.CreateDatabaseContext();
			var databaseCreator = (SqlDatabaseCreator) this.SystemDataBasedDatabaseConnection.NewDatabaseCreator(this.Model);
            
			foreach (var migrationTypeInfo in databaseMigrationPlan.NewTypes)
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

			foreach (var migrationTypeInfo in databaseMigrationPlan.ModifiedTypes)
			{
				retval.AddScript(this.Model, migrationTypeInfo.TypeDescriptor, CreateMigrationScript(migrationTypeInfo));
			}

			return retval;
		}
	}
}
