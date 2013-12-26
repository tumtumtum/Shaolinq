// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Platform;
using Platform.Reflection;
using Platform.Validation;
using log4net;

namespace Shaolinq.Persistence.Sql
{
	public abstract class SqlDatabaseCreator
		: DatabaseCreator
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(SqlDatabaseCreator).Name);

		#region CreateDatabaseContext

		public class CreateDatabaseContext
		{
			private readonly List<string> ammendments;
			private readonly Dictionary<string, string> manyToManyTablesCreated;

			public CreateDatabaseContext()
			{
				this.ammendments = new List<string>();
				this.manyToManyTablesCreated = new Dictionary<string, string>();
			}

			public IEnumerable<string> GetAmmendmentStrings()
			{
				return ammendments;
			}

			public void AddAmmendment(string ammendment)
			{
				this.ammendments.Add(ammendment);
			}

			public void ApplyAmmendments(SqlDatabaseTransactionContext context)
			{
				foreach (string ammendment in ammendments)
				{
					using (var command = context.DbConnection.CreateCommand())
					{
						if (Log.IsDebugEnabled)
						{
							Log.DebugFormat(ammendment);
						}

						command.CommandText = ammendment;

						try
						{
							command.ExecuteScalar();
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
						}
					}
				}
			}

			public void AddManyToManyTable(string name)
			{
				this.manyToManyTablesCreated[name] = name;
			}

			public bool HasManyToManyTable(string name)
			{
				return this.manyToManyTablesCreated.ContainsKey(name);
			}
		}

		#endregion

		public DataAccessModel Model { get; private set; }

		protected SystemDataBasedDatabaseConnection SystemDataBasedDatabaseConnection { get; private set; }

		public DataAccessModelDatabaseConnectionInfo DatabaseConnectionInfo { get; private set; }

		private readonly TypeDescriptorProvider typeDescriptorProvider;

		public ModelTypeDescriptor ModelTypeDescriptor { get; private set; }

		private readonly SqlSchemaWriter schemaWriter;
		private readonly string identifierQuoteString;

		protected SqlDatabaseCreator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
		{
			this.SystemDataBasedDatabaseConnection = databaseConnection;
			this.Model = model;
			this.typeDescriptorProvider = TypeDescriptorProvider.GetProvider(model.DefinitionAssembly);
			this.ModelTypeDescriptor = this.Model.ModelTypeDescriptor;

			this.identifierQuoteString = databaseConnection.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.IdentifierQuote);
			this.schemaWriter = databaseConnection.NewSqlSchemaWriter(model);
		}

		public override bool CreateDatabase(bool overwrite)
		{
			var createDatabaseContext = new CreateDatabaseContext();

			if (!this.SystemDataBasedDatabaseConnection.CreateDatabase(overwrite))
			{
				return false;
			}

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
								CreateTable(createDatabaseContext, (SqlDatabaseTransactionContext)dataTransactionContext, typeDescriptor);
							}

							createDatabaseContext.ApplyAmmendments((SqlDatabaseTransactionContext)dataTransactionContext);
						}
					}
					else
					{
						throw new NotSupportedException(String.Format("DatabaseConnection '{0}' does not support SupportsDisabledForeignKeyCheckContext", this.SystemDataBasedDatabaseConnection.GetType()));
					}

					scope.Complete();
				}

				return true;
			}
		}

		/// <summary>
		/// Creates SQL table for the given type.
		/// </summary>
		public virtual void CreateTable(CreateDatabaseContext createDatabaseContext, SqlDatabaseTransactionContext databaseTransactionContext, TypeDescriptor typeDescriptor)
		{
			foreach (var commandText in GetCreateStrings(createDatabaseContext, typeDescriptor))
			{
				using (var command = databaseTransactionContext.DbConnection.CreateCommand())
				{
					command.CommandText = commandText;

					if (Log.IsDebugEnabled)
					{
						Log.Debug(command.CommandText);
					}

					try
					{
						command.ExecuteScalar();
					}
					catch (Exception e)
					{
						Log.Error(e.Message);

						throw;
					}
				}
			}
		}

		/// <summary>
		/// Gets an enumeration of sql command strings for creating a table from a type.
		/// </summary>
		/// <returns>An enumeration of strings</returns>
		public virtual IEnumerable<string> GetCreateStrings(CreateDatabaseContext createDatabaseContext, TypeDescriptor typeDescriptor)
		{
			int snip = 0;
			var builder = new StringBuilder();

			builder.AppendFormat(@"CREATE TABLE {0}{1}{0}", identifierQuoteString, typeDescriptor.GetPersistedName(this.Model));

			builder.AppendLine();
			builder.AppendLine("(");

			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				this.schemaWriter.WriteColumnDefinition(builder, propertyDescriptor, null, false);

				builder.AppendLine(",");
				snip = Environment.NewLine.Length + 1;
			}

			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos())
			{
				if (typeRelationshipInfo.EntityRelationshipType == EntityRelationshipType.ChildOfOneToMany)
				{
					var relatedPropertyTypeDescriptor = this.ModelTypeDescriptor.GetQueryableTypeDescriptor(typeRelationshipInfo.RelatedProperty.PropertyType);

					var valueRequired = true;
					var valueRequiredAttribute = typeRelationshipInfo.RelatedProperty.PropertyInfo.GetFirstCustomAttribute<ValueRequiredAttribute>(false);

					if (valueRequiredAttribute != null && !valueRequiredAttribute.Required)
					{
						valueRequired = false;
					}

					foreach (var primaryKeyProperty in relatedPropertyTypeDescriptor.PrimaryKeyProperties)
					{
						this.schemaWriter.AppendForeignKeyColumnDefinition(typeRelationshipInfo.RelatedProperty.PersistedName + primaryKeyProperty.PersistedShortName, typeRelationshipInfo.RelatedTypeTypeDescriptor, builder, valueRequired);
					}

					builder.AppendLine(",");
					snip = Environment.NewLine.Length + 1;
				}
			}

			builder.Length -= snip;

			// Primary Key

			if (typeDescriptor.PrimaryKeyCount > 1)
			{
				string primaryKeys = typeDescriptor.PrimaryKeyProperties
												   .Convert<PropertyDescriptor, string>(x => identifierQuoteString + x.PersistedName + identifierQuoteString)
				                                   .JoinToString(", ");

				builder.AppendLine(",");
				builder.AppendFormat("PRIMARY KEY ({0})", primaryKeys);
			}

			if (this.SystemDataBasedDatabaseConnection.SqlDialect.SupportsFeature(SqlFeature.Constraints))
			{
				foreach (var propertyDescriptor in typeDescriptor.PersistedProperties.Where(c => c.PropertyType.IsDataAccessObjectType()))
				{
					var propertyTypeDescriptor = this.Model.GetTypeDescriptor(propertyDescriptor.PropertyType);

					var names = propertyTypeDescriptor.PrimaryKeyProperties.Select(relatedPropertyDescriptor => propertyDescriptor.PersistedName + relatedPropertyDescriptor.PersistedShortName).ToList();
					var foreignKeyNames = names.JoinToString(", "); 
					names = propertyTypeDescriptor.PrimaryKeyProperties.Select(relatedPropertyDescriptor => relatedPropertyDescriptor.PersistedName).ToList();
					var primaryKeyNames = names.JoinToString(", ");

					if (this.SystemDataBasedDatabaseConnection.SqlDialect.SupportsFeature(SqlFeature.AlterTableAddConstraints))
					{
						var s = string.Format
						(
							"ALTER TABLE {3}{6}{3} ADD CONSTRAINT {0}_{1}_Fkc FOREIGN KEY ({3}{0}{3}) REFERENCES {3}{1}{3}({3}{2}{3}) ON DELETE SET {5} {4}",
							foreignKeyNames,
							propertyTypeDescriptor.GetPersistedName(this.Model),
							primaryKeyNames,
							identifierQuoteString,
							this.SystemDataBasedDatabaseConnection.SqlDialect.DeferrableText,
							(!propertyTypeDescriptor.Type.IsValueType || Nullable.GetUnderlyingType(propertyTypeDescriptor.Type) != null) ? "NULL" : "DEFAULT",
							typeDescriptor.GetPersistedName(this.Model)
						);

						createDatabaseContext.AddAmmendment(s);
					}
					else
					{
						var s = string.Format
						(
							"FOREIGN KEY ({3}{0}{3}) REFERENCES {3}{1}{3}({3}{2}{3}) ON DELETE SET {5} {4}",
							foreignKeyNames,
							propertyTypeDescriptor.GetPersistedName(this.Model),
							primaryKeyNames,
							identifierQuoteString,
							this.SystemDataBasedDatabaseConnection.SqlDialect.DeferrableText,
							(!propertyTypeDescriptor.Type.IsValueType || Nullable.GetUnderlyingType(propertyTypeDescriptor.Type) != null) ? "NULL" : "DEFAULT"
						);

						builder.AppendLine(",");
						builder.Append(s);
					}
				}
			}

			// Foreign key references

			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos().Filter(f => f.EntityRelationshipType == EntityRelationshipType.ChildOfOneToMany))
			{
				var relatedPropertyTypeDescriptor = this.typeDescriptorProvider.GetTypeDescriptor(typeRelationshipInfo.RelatedProperty.PropertyType);

				var relatedPrimaryKeys = typeRelationshipInfo
					.RelatedTypeTypeDescriptor.PrimaryKeyProperties
					.Convert<PropertyDescriptor, string>(x => x.PersistedName)
					.JoinToString(", ");

				var foreignKeys = typeRelationshipInfo
					.RelatedTypeTypeDescriptor.PrimaryKeyProperties
					.Convert<PropertyDescriptor, string>(x => typeRelationshipInfo.RelatedProperty.PersistedName + x.PersistedShortName)
					.JoinToString(", ");

				if (this.SystemDataBasedDatabaseConnection.SqlDialect.SupportsFeature(SqlFeature.Constraints))
				{
					/*
					var ammendment =
						string.Format
							(
							"ALTER TABLE {5}{0}{5} ADD CONSTRAINT {0}_{1}_Fkc FOREIGN KEY ({5}{2}{5}) REFERENCES {5}{3}{5}({5}{4}{5}) {6}",
							typeDescriptor.GetPersistedName(this.Model),
							typeRelationshipInfo.RelatedTypeTypeDescriptor.GetPersistedName(this.Model),
							foreignKeys,
							typeRelationshipInfo.RelatedTypeTypeDescriptor.GetPersistedName(this.Model),
							relatedPrimaryKeys,
							this.databaseConnection.SqlDialect.NameQuoteChar,
							this.databaseConnection.SqlDialect.DeferrableText
							);

					createDatabaseContext.AddAmmendment(ammendment);

					builder.AppendLine(ammendment);*/

					var s = string.Format
						(
							"FOREIGN KEY ({3}{0}{3}) REFERENCES {3}{1}{3}({3}{2}{3}) {4}",
							foreignKeys,
							typeRelationshipInfo.RelatedTypeTypeDescriptor.GetPersistedName(this.Model),
							relatedPrimaryKeys,
							identifierQuoteString,
							this.SystemDataBasedDatabaseConnection.SqlDialect.DeferrableText
						);

					builder.AppendLine(",");
					builder.Append(s);
				}
			}

			builder.AppendLine();
			builder.AppendLine(");");

			yield return builder.ToString();

			builder.Length = 0;

			// Append table for many-to-many relationships if it has not already been created

			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos())
			{
				if (typeRelationshipInfo.EntityRelationshipType == EntityRelationshipType.ManyToMany)
				{
					var manyToManyTableName = CreateManyToManyTableName(this.Model, typeDescriptor, typeRelationshipInfo.RelatedTypeTypeDescriptor);

					if (!createDatabaseContext.HasManyToManyTable(manyToManyTableName))
					{
						createDatabaseContext.AddManyToManyTable(manyToManyTableName);

						var type1 = typeDescriptor;
						var type2 = typeRelationshipInfo.RelatedTypeTypeDescriptor;

						if (StringComparer.InvariantCulture.Compare(type1.GetPersistedName(this.Model), type2.GetPersistedName(this.Model)) >= 0)
						{
							MathUtils.Swap<TypeDescriptor>(ref type1, ref type2);
						}

						var dataType = this.SystemDataBasedDatabaseConnection.SqlDataTypeProvider.GetSqlDataType(typeof(int));

						builder.AppendFormat(@"CREATE TABLE {0}{1}{0}", identifierQuoteString, manyToManyTableName);
						builder.AppendLine("(");
						builder.Append("Id ");
						builder.Append(dataType.GetSqlName(null));
						builder.Append(" PRIMARY KEY ");
						builder.Append(this.AutoIncrementKeyword);
						builder.AppendLine(", ");
						this.schemaWriter.AppendForeignKeyColumnDefinition(null, type1, builder, true);
						builder.AppendLine(",");
						this.schemaWriter.AppendForeignKeyColumnDefinition(null, type2, builder, true);

						builder.AppendLine(");");

						yield return builder.ToString();
					}
				}
			}

			// Indexes

			foreach (var indexDescriptor in typeDescriptor.Indexes)
			{
				var ammendment = new StringBuilder();

				this.schemaWriter.WriteCreateIndex(ammendment, typeDescriptor, indexDescriptor);

				createDatabaseContext.AddAmmendment(ammendment.ToString());
			}
		}

		private string AutoIncrementKeyword
		{
			get
			{
				return this.SystemDataBasedDatabaseConnection.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.AutoIncrementSuffix);
			}
		}

		public static string CreateManyToManyTableName(DataAccessModel model, TypeDescriptor type1, TypeDescriptor type2)
		{
			if (StringComparer.InvariantCulture.Compare(type1.GetPersistedName(model), type2.GetPersistedName(model)) >= 0)
			{
				MathUtils.Swap<TypeDescriptor>(ref type1, ref type2);
			}

			return type1.GetPersistedName(model) + "_" + type2.GetPersistedName(model);
		}
	}
}
