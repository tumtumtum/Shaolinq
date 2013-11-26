// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Text;
using Platform;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence.Sql
{
	public class SqlSchemaWriter
	{
		public BaseDataAccessModel Model { get; set; }
		public SqlPersistenceContext SqlPersistenceContext { get; private set; }
		public DataAccessModelPersistenceContextInfo PersistenceContextInfo { get; set; }

		public SqlSchemaWriter(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			this.SqlPersistenceContext = sqlPersistenceContext;
			this.Model = model;
			this.PersistenceContextInfo = persistenceContextInfo;
		}

		public static string CreateForiegnKeyName(string foriegnTableName, string idColumnName)
		{
			if (idColumnName == "Id")
			{
				return foriegnTableName + idColumnName;
			}
			else
			{
				return idColumnName;
			}
		}

		public virtual void AppendForeignKeyColumnDefinition(string name, TypeDescriptor relatedType, StringBuilder builder, bool valueRequired)
		{
			int count = 0;

			foreach (var propertyDescriptor in relatedType.PrimaryKeyProperties)
			{
				count++;

				var columnName = name ?? CreateForiegnKeyName(relatedType.GetPersistedName(this.Model), propertyDescriptor.PersistedName);

				var dataType = this.SqlPersistenceContext.SqlDataTypeProvider.GetSqlDataType(propertyDescriptor.PropertyType);

				builder.Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(columnName).Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(" ");
				builder.Append(this.SqlPersistenceContext.SqlDialect.GetColumnName(propertyDescriptor, dataType, true)).Append(" ");

				if (valueRequired)
				{
					builder.Append("NOT NULL");
				}
			}

			if (count == 0)
			{
				throw new SqlDatabaseCreationException(String.Format("Entity in a relationship ({0}) is missing a primary key", relatedType.GetPersistedName(this.Model)));
			}
		}

		public virtual void WriteCreateIndex(StringBuilder builder, TypeDescriptor typeDescriptor, IndexDescriptor indexDescriptor)
		{
			PrivateWriteCreateIndex(builder, typeDescriptor, indexDescriptor, indexDescriptor.AlsoIndexToLower);
		}

		public virtual string GetQualifiedIndexName(TypeDescriptor typeDescriptor, IndexDescriptor indexDescriptor)
		{
			var indexName = indexDescriptor.Name;
			var typeNameForIndex = typeDescriptor.GetPersistedName(this.Model.GetPersistenceContext(this.PersistenceContextInfo.ContextName));
			
			if (!this.SqlPersistenceContext.SqlDialect.SupportsIndexNameCasing)
			{
				indexName = indexName.ToLower();
				typeNameForIndex = typeNameForIndex.ToLower();
			}

			return typeNameForIndex + "_" + indexName + "_" + "idx";
		}

		public virtual void PrivateWriteCreateIndex(StringBuilder builder, TypeDescriptor typeDescriptor, IndexDescriptor indexDescriptor, bool toLower)
		{
			builder.Append("CREATE ");

			if (indexDescriptor.IsUnique)
			{
				builder.Append("UNIQUE ");
			}

			builder.Append("INDEX ");

			builder.Append(GetQualifiedIndexName(typeDescriptor, indexDescriptor));
			builder.Append(' ');
			builder.AppendFormat("ON {0}{1}{0}", this.SqlPersistenceContext.SqlDialect.NameQuoteChar, typeDescriptor.GetPersistedName(this.Model));
			builder.Append('(');

			foreach (var propertyDescriptor in indexDescriptor.Properties)
			{
				if (toLower)
				{
					builder.Append("(lower(");
					builder.Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(propertyDescriptor.PersistedName).Append(
						this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append("))");
					builder.Append(',');
				}
				else
				{
					builder.Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(propertyDescriptor.PersistedName).Append(
						this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(" ");
					builder.Append(',');
				}
			}

			builder.Length--;

			builder.Append(");");
		}

		public virtual IEnumerable<string> GetPersistedNames(PropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				foreach (var v in this.SqlPersistenceContext.GetPersistedNames(this.Model, propertyDescriptor))
				{
					yield return v.Left;
				}

				yield break;
			}
			
			yield return propertyDescriptor.PersistedName;
		}

		public virtual void WriteColumnDefinition(StringBuilder builder, PropertyDescriptor propertyDescriptor, string name, bool asForeignKey)
		{
			if (name == null)
			{
				name = propertyDescriptor.PersistedName;
			}

			if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				foreach (var v in this.SqlPersistenceContext.GetPersistedNames(this.Model, propertyDescriptor))
				{
					this.WriteColumnDefinition(builder, v.Right, v.Left, true);
				}

				return;
			}

			var dataType = this.SqlPersistenceContext.SqlDataTypeProvider.GetSqlDataType(propertyDescriptor.PropertyType);

			builder.Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(name).Append(this.SqlPersistenceContext.SqlDialect.NameQuoteChar).Append(" ");
			builder.Append(this.SqlPersistenceContext.SqlDialect.GetColumnName(propertyDescriptor, dataType, asForeignKey));

			bool requiresNotNull = false;

			// Is not a value type 
			if (!propertyDescriptor.PropertyType.IsValueType || asForeignKey)
			{
				// Nullability is determined by ValueRequired attribute
				var valueRequiredAttribute = propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<ValueRequiredAttribute>(false);

				if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
				{
					requiresNotNull = true;
				}
			}
			else
			{
				// Is value type.  Nullable if type is a nullable type
				if (Nullable.GetUnderlyingType(propertyDescriptor.PropertyType) == null)
				{
					requiresNotNull = true;
				}
			}

			if (propertyDescriptor.HasUniqueAttribute && propertyDescriptor.UniqueAttribute.Unique)
			{
				builder.Append(" UNIQUE");
			}

			if (requiresNotNull)
			{
				builder.Append(" NOT NULL");
			}

			if (propertyDescriptor.IsPrimaryKey)
			{
				if (propertyDescriptor.DeclaringTypeDescriptor.PrimaryKeyCount == 1 && !asForeignKey)
				{
					if (requiresNotNull)
					{
						builder.Length -= " NOT NULL".Length;
					}

					builder.Append(" PRIMARY KEY ");
				}

				if (propertyDescriptor.PropertyType.IsIntegerType()
					&& propertyDescriptor.IsAutoIncrement
					&& !asForeignKey)
				{
					builder.Append(" ");
					builder.Append(this.SqlPersistenceContext.SqlDialect.GetAutoIncrementSuffix());
				}
			}

			var defaultValueAttribute = propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<DefaultValueAttribute>(true);

			if (defaultValueAttribute != null)
			{
				builder.AppendFormat(" DEFAULT {0} ", defaultValueAttribute.Value);
			}
		}
	}
}
