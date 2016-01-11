// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlDataDefinitionExpressionBuilder
	{
		private readonly SqlDialect sqlDialect;
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private readonly DataAccessModel model;
		private readonly DatabaseCreationOptions options;
		private readonly string tableNamePrefix;
		private List<Expression> currentTableConstraints;
		private readonly SqlDataDefinitionBuilderFlags flags;
		
		private SqlDataDefinitionExpressionBuilder(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, DataAccessModel model, DatabaseCreationOptions options, string tableNamePrefix, SqlDataDefinitionBuilderFlags flags)
		{
			this.model = model;
			this.options = options;
			this.sqlDialect = sqlDialect;
			this.tableNamePrefix = tableNamePrefix;
			this.flags = flags;
			this.sqlDataTypeProvider = sqlDataTypeProvider;

			this.currentTableConstraints = new List<Expression>();
		}

		private List<Expression> BuildColumnConstraints(PropertyDescriptor propertyDescriptor, PropertyDescriptor foreignKeyReferencingProperty)
		{
			var retval = new List<Expression>();

			if (foreignKeyReferencingProperty != null)
			{
				var valueRequiredAttribute = foreignKeyReferencingProperty.ValueRequiredAttribute;

				if (foreignKeyReferencingProperty.HasUniqueAttribute && foreignKeyReferencingProperty.UniqueAttribute.Unique)
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique));
				}

				if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
				}
			}
			else
			{
				if (propertyDescriptor.PropertyType.IsNullableType() || !propertyDescriptor.PropertyType.IsValueType)
				{
					var valueRequiredAttribute = propertyDescriptor.ValueRequiredAttribute;

					if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
					{
						retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
					}
				}
				else
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
				}

				if (propertyDescriptor.IsAutoIncrement && propertyDescriptor.PropertyType.IsIntegerType(true))
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.AutoIncrement));
				}

				if (propertyDescriptor.HasUniqueAttribute && propertyDescriptor.UniqueAttribute.Unique)
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.Unique));
				}
				
				var defaultValueAttribute = propertyDescriptor.DefaultValueAttribute;

				if (defaultValueAttribute != null)
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.DefaultValue, null, defaultValueAttribute.Value));
				}
			}

			return retval;
		}

		private SqlColumnReferenceAction FixAction(SqlColumnReferenceAction action)
		{
			switch (action)
			{
			case SqlColumnReferenceAction.Cascade:
				return this.sqlDialect.SupportsCapability(SqlCapability.RestrictAction) ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.NoAction;
			case SqlColumnReferenceAction.NoAction:
				return SqlColumnReferenceAction.NoAction;
			case SqlColumnReferenceAction.Restrict:
				return this.sqlDialect.SupportsCapability(SqlCapability.RestrictAction) ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.NoAction;
			case SqlColumnReferenceAction.SetDefault:
				return this.sqlDialect.SupportsCapability(SqlCapability.SetDefaultAction) ? SqlColumnReferenceAction.SetDefault : SqlColumnReferenceAction.NoAction;
			case SqlColumnReferenceAction.SetNull:
				return this.sqlDialect.SupportsCapability(SqlCapability.SetNullAction) ? SqlColumnReferenceAction.SetNull : SqlColumnReferenceAction.NoAction;
			default:
				throw new ArgumentOutOfRangeException(nameof(action));
			}
		}

		private SqlColumnReferenceAction? ToSqlColumnReferenceAction(ForeignObjectAction foreignObjectAction)
		{
			switch (foreignObjectAction)
			{
			case ForeignObjectAction.Default:
				return null;
			case ForeignObjectAction.NoAction:
				return SqlColumnReferenceAction.NoAction;
			case ForeignObjectAction.Restrict:
				return SqlColumnReferenceAction.Restrict;
			case ForeignObjectAction.Cascade:
				return SqlColumnReferenceAction.Cascade;
			case ForeignObjectAction.SetNull:
				return SqlColumnReferenceAction.SetNull;
			case ForeignObjectAction.SetDefault:
				return SqlColumnReferenceAction.SetDefault;
			default:
				throw new ArgumentOutOfRangeException(nameof(foreignObjectAction));
			}
		}

		private IEnumerable<SqlColumnDefinitionExpression> BuildForeignKeyColumnDefinitions(PropertyDescriptor referencingProperty, ColumnInfo[] columnInfos)
		{
			var relatedPropertyTypeDescriptor = this.model.GetTypeDescriptor(referencingProperty.PropertyType);
			var referencedTableName = relatedPropertyTypeDescriptor.PersistedName;

			var valueRequired = (referencingProperty.ValueRequiredAttribute != null && referencingProperty.ValueRequiredAttribute.Required)
				|| referencingProperty.IsPrimaryKey;
			var supportsInlineForeignKeys = this.sqlDialect.SupportsCapability(SqlCapability.InlineForeignKeys);

			var foreignObjectConstraintAttribute = referencingProperty.ForeignObjectConstraintAttribute;

			foreach (var foreignKeyColumn in columnInfos)
			{
				var retval = this.BuildColumnDefinition(foreignKeyColumn);

				if (columnInfos.Length == 1 && supportsInlineForeignKeys)
				{
					var names = new[] { foreignKeyColumn.DefinitionProperty.PersistedName };
					var newConstraints = new List<Expression>(retval.ConstraintExpressions);

					var referencesColumnExpression = new SqlReferencesColumnExpression
					(
						new SqlTableExpression(referencedTableName),
						SqlColumnReferenceDeferrability.InitiallyDeferred,
						names, this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction).Value : (valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull)), this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnUpdateAction).Value : SqlColumnReferenceAction.NoAction)
					);

					newConstraints.Add(referencesColumnExpression);

					retval = new SqlColumnDefinitionExpression(retval.ColumnName, retval.ColumnType, newConstraints);
				}

				yield return retval;
			}

			if (columnInfos.Length > 1 || !supportsInlineForeignKeys)
			{
				var currentTableColumnNames = columnInfos.Select(c => c.ColumnName);
				var referencedTableColumnNames = columnInfos.Select(c => c.GetTailColumnName());
				
				var referencesColumnExpression = new SqlReferencesColumnExpression
				(
					new SqlTableExpression(referencedTableName),
					SqlColumnReferenceDeferrability.InitiallyDeferred,
					referencedTableColumnNames, this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction).Value : (valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull)), this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnUpdateAction).Value : SqlColumnReferenceAction.NoAction)
				);

				var foreignKeyConstraint = new SqlForeignKeyConstraintExpression(null, currentTableColumnNames, referencesColumnExpression);

				this.currentTableConstraints.Add(foreignKeyConstraint);
			}
		}

		private SqlColumnDefinitionExpression BuildColumnDefinition(ColumnInfo columnInfo)
		{
			var sqlDataType = this.sqlDataTypeProvider.GetSqlDataType(columnInfo.DefinitionProperty.PropertyType);
			var columnDataTypeName = sqlDataType.GetSqlName(columnInfo.DefinitionProperty);
			var constraints = this.BuildColumnConstraints(columnInfo.DefinitionProperty,  columnInfo.VisitedProperties.FirstOrDefault());

			return new SqlColumnDefinitionExpression(columnInfo.ColumnName, new SqlTypeExpression(columnDataTypeName, sqlDataType.IsUserDefinedType), constraints);
		}

		private IEnumerable<SqlColumnDefinitionExpression> BuildRelatedColumnDefinitions(TypeDescriptor typeDescriptor)
		{
			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos())
			{
				if (typeRelationshipInfo.RelationshipType == RelationshipType.ChildOfOneToMany)
				{
					var foreignKeyColumns = QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, typeRelationshipInfo.ReferencingProperty);

					foreach (var result in this.BuildForeignKeyColumnDefinitions(typeRelationshipInfo.ReferencingProperty, foreignKeyColumns))
					{
						yield return result;
					}
				}
			}
		}

		private Expression BuildCreateTableExpression(TypeDescriptor typeDescriptor)
		{
			var columnExpressions = new List<SqlColumnDefinitionExpression>();

			this.currentTableConstraints = new List<Expression>();

			var columnInfos = QueryBinder.GetColumnInfos
			(
				this.model.TypeDescriptorProvider,
				typeDescriptor.PersistedAndRelatedObjectProperties,
				(c, d) => c.IsPrimaryKey && !c.PropertyType.IsDataAccessObjectType(),
				(c, d) => c.IsPrimaryKey
			);

			foreach (var columnInfo in columnInfos)
			{
				columnExpressions.Add(this.BuildColumnDefinition(columnInfo));
			}

			columnInfos = QueryBinder.GetColumnInfos
			(
				this.model.TypeDescriptorProvider,
				typeDescriptor.PersistedProperties.Where(c => !c.PropertyType.IsDataAccessObjectType()),
				(c, d) => d == 0 ? !c.IsPrimaryKey : c.IsPrimaryKey,
				(c, d) => d == 0 ? !c.IsPrimaryKey : c.IsPrimaryKey
			);

			foreach (var columnInfo in columnInfos)
			{
				columnExpressions.Add(this.BuildColumnDefinition(columnInfo));
			}

			foreach (var property in typeDescriptor.PersistedProperties
				.Where(c => c.PropertyType.IsDataAccessObjectType()))
			{
				columnInfos = QueryBinder.GetColumnInfos
				(
					this.model.TypeDescriptorProvider,
					new [] { property },
					(c, d) => d == 0 || c.IsPrimaryKey,
					(c, d) => c.IsPrimaryKey
				);

				columnExpressions.AddRange(this.BuildForeignKeyColumnDefinitions(property, columnInfos));
			}

			columnExpressions.AddRange(this.BuildRelatedColumnDefinitions(typeDescriptor));

			var tableName = typeDescriptor.PersistedName;

			var primaryKeys = QueryBinder.GetPrimaryKeyColumnInfos(this.model.TypeDescriptorProvider, typeDescriptor);

			if (primaryKeys.Length > 0)
			{
				var columnNames = primaryKeys.Select(c => c.ColumnName).ToArray();

				var compositePrimaryKeyConstraint = new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKey, columnNames);

				this.currentTableConstraints.Add(compositePrimaryKeyConstraint);
			}

			return new SqlCreateTableExpression(new SqlTableExpression(tableName), false, columnExpressions, this.currentTableConstraints, Enumerable.Empty<SqlTableOption>());
		}

		private Expression BuildIndexExpression(SqlTableExpression table, string indexName, Tuple<IndexAttribute, PropertyDescriptor>[] properties)
		{
			var unique = properties.Select(c => c.Item1).Any(c => c.Unique);
			var lowercaseIndex = properties.Select(c => c.Item1).Any(c => c.LowercaseIndex);
			var indexType = properties.Select(c => c.Item1.IndexType).FirstOrDefault(c => c != IndexType.Default);

			var sorted = properties.OrderBy(c => c.Item1.CompositeOrder, Comparer<int>.Default);

			var indexedColumns = new List<SqlIndexedColumnExpression>();

			foreach (var attributeAndProperty in sorted)
			{
				foreach (var columnInfo in QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, attributeAndProperty.Item2))
				{
					indexedColumns.Add(new SqlIndexedColumnExpression(new SqlColumnExpression(columnInfo.DefinitionProperty.PropertyType, null, columnInfo.ColumnName), attributeAndProperty.Item1.SortOrder, attributeAndProperty.Item1.LowercaseIndex));
				}
			}
				
			return new SqlCreateIndexExpression(indexName, table, unique, lowercaseIndex, indexType, false, indexedColumns);
		}

		private IEnumerable<Expression> BuildCreateIndexExpressions(TypeDescriptor typeDescriptor)
		{
			var allIndexAttributes = typeDescriptor.PersistedProperties.Concat(typeDescriptor.RelatedProperties).SelectMany(c => c.IndexAttributes.Select(d => new Tuple<IndexAttribute, PropertyDescriptor>(d, c)));

			var indexAttributesByName = allIndexAttributes.GroupBy(c => c.Item1.IndexName ?? typeDescriptor.PersistedName + "_" + c.Item2.PersistedName + "_idx").Sorted((x, y) => String.CompareOrdinal(x.Key, y.Key));

			var table = new SqlTableExpression(typeDescriptor.PersistedName);

			foreach (var group in indexAttributesByName)
			{
				var indexName = group.Key;

				var propertyDescriptors = group.ToArray();

				yield return this.BuildIndexExpression(table, indexName, propertyDescriptors);
			}
		}

		private Expression Build()
		{
			var expressions = new List<Expression>();

			if ((this.flags & SqlDataDefinitionBuilderFlags.BuildEnums) != 0)
			{
				foreach (var enumTypeDescriptor in this.model.TypeDescriptorProvider.GetPersistedEnumTypeDescriptors())
				{
					expressions.Add(this.BuildCreateEnumTypeExpression(enumTypeDescriptor));
				}
			}

			if ((this.flags & (SqlDataDefinitionBuilderFlags.BuildIndexes | SqlDataDefinitionBuilderFlags.BuildIndexes)) != 0)
			{
				foreach (var typeDescriptor in this.model.TypeDescriptorProvider.GetPersistedObjectTypeDescriptors())
				{
					expressions.Add(this.BuildCreateTableExpression(typeDescriptor));
					expressions.AddRange(this.BuildCreateIndexExpressions(typeDescriptor));
				}
			}
			else
			{
				if ((this.flags & (SqlDataDefinitionBuilderFlags.BuildIndexes)) != 0)
				{
					foreach (var typeDescriptor in this.model.TypeDescriptorProvider.GetPersistedObjectTypeDescriptors())
					{
						expressions.AddRange(this.BuildCreateIndexExpressions(typeDescriptor));
					}
				}

				if ((this.flags & (SqlDataDefinitionBuilderFlags.BuildTables)) != 0)
				{
					foreach (var typeDescriptor in this.model.TypeDescriptorProvider.GetPersistedObjectTypeDescriptors())
					{
						expressions.Add(this.BuildCreateTableExpression(typeDescriptor));
					}
				}
			}

			return new SqlStatementListExpression(expressions);
		}

		private Expression BuildCreateEnumTypeExpression(EnumTypeDescriptor enumTypeDescriptor)
		{
			var sqlTypeExpression = new SqlTypeExpression(enumTypeDescriptor.Name, true);
			var asExpression = new SqlEnumDefinitionExpression(enumTypeDescriptor.GetValues());

			return new SqlCreateTypeExpression(sqlTypeExpression, asExpression, true);
		}

		public static Expression Build(SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, DataAccessModel model, DatabaseCreationOptions options, string tableNamePrefix, SqlDataDefinitionBuilderFlags flags)
		{
			var builder = new SqlDataDefinitionExpressionBuilder(sqlDialect, sqlDataTypeProvider, model, options, tableNamePrefix, flags);

			var retval = builder.Build();

			return retval;
		}
	}
}
