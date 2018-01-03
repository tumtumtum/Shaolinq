// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Computed;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class SqlDataDefinitionExpressionBuilder
	{
		private readonly SqlDialect sqlDialect;
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private readonly DataAccessModel dataAccessModel;
		private readonly SqlQueryFormatterManager formatterManager;
		private readonly DataAccessModel model;
		private List<SqlConstraintExpression> currentTableConstraints;
		private readonly SqlDataDefinitionBuilderFlags flags;
		
		private SqlDataDefinitionExpressionBuilder(DataAccessModel dataAccessModel, SqlQueryFormatterManager formatterManager, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, DataAccessModel model, DatabaseCreationOptions options, string tableNamePrefix, SqlDataDefinitionBuilderFlags flags)
		{
			this.dataAccessModel = dataAccessModel;
			this.formatterManager = formatterManager;
			this.model = model;
			this.sqlDialect = sqlDialect;
			this.flags = flags;
			this.sqlDataTypeProvider = sqlDataTypeProvider;
			this.currentTableConstraints = new List<SqlConstraintExpression>();
		}

		private List<SqlConstraintExpression> BuildColumnConstraints(PropertyDescriptor propertyDescriptor, PropertyDescriptor foreignKeyReferencingProperty)
		{
			var retval = new List<SqlConstraintExpression>();

			if (foreignKeyReferencingProperty != null)
			{
				var valueRequiredAttribute = foreignKeyReferencingProperty.ValueRequiredAttribute;

				if (foreignKeyReferencingProperty.HasUniqueAttribute && foreignKeyReferencingProperty.UniqueAttribute.Unique)
				{
					retval.Add(new SqlConstraintExpression(ConstraintType.Unique));
				}

				if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
				{
					retval.Add(new SqlConstraintExpression(ConstraintType.NotNull));
				}
			}
			else
			{
				if (propertyDescriptor.PropertyType.IsNullableType() || !propertyDescriptor.PropertyType.IsValueType)
				{
					var valueRequiredAttribute = propertyDescriptor.ValueRequiredAttribute;

					if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
					{
						retval.Add(new SqlConstraintExpression(ConstraintType.NotNull));
					}
				}
				else
				{
					retval.Add(new SqlConstraintExpression(ConstraintType.NotNull));
				}

				if (propertyDescriptor.IsAutoIncrement && propertyDescriptor.PropertyType.IsIntegerType(true))
				{
					retval.Add(new SqlConstraintExpression(ConstraintType.AutoIncrement, null, new object[] { propertyDescriptor.AutoIncrementAttribute.Seed, propertyDescriptor.AutoIncrementAttribute.Step }));
				}

				if (propertyDescriptor.HasUniqueAttribute && propertyDescriptor.UniqueAttribute.Unique)
				{
					retval.Add(new SqlConstraintExpression(ConstraintType.Unique));
				}

				if (propertyDescriptor.HasDefaultValue)
				{
					var outputDefaultValue = propertyDescriptor.HasExplicitDefaultValue
						|| (propertyDescriptor.HasImplicitDefaultValue && model.Configuration.IncludeImplicitDefaultsInSchema && propertyDescriptor.DefaultValue != null);
					
					if (outputDefaultValue)
					{
						retval.Add(new SqlConstraintExpression(ConstraintType.DefaultValue, constraintName: this.formatterManager.GetDefaultValueConstraintName(propertyDescriptor), defaultValue: Expression.Constant(propertyDescriptor.DefaultValue)));
					}
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

				if (columnInfos.Length == 1 && supportsInlineForeignKeys && !(referencingProperty.ForeignObjectConstraintAttribute?.Disabled ?? false))
				{
					var names = new[] { foreignKeyColumn.DefinitionProperty.PersistedName };
					var newConstraints = new List<SqlConstraintExpression>(retval.ConstraintExpressions);

					var referencesColumnExpression = new SqlReferencesExpression
					(
						new SqlTableExpression(referencedTableName),
						SqlColumnReferenceDeferrability.InitiallyDeferred,
						names, this.FixAction(foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction).Value : (valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull)), this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnUpdateAction).Value : SqlColumnReferenceAction.NoAction)
					);

					newConstraints.Add(new SqlConstraintExpression(referencesColumnExpression, this.formatterManager.GetForeignKeyConstraintName(referencingProperty)));

					retval = new SqlColumnDefinitionExpression(retval.ColumnName, retval.ColumnType, newConstraints);
				}

				yield return retval;
			}

			if ((columnInfos.Length > 1 || !supportsInlineForeignKeys) && !(referencingProperty.ForeignObjectConstraintAttribute?.Disabled ?? false))
			{
				var currentTableColumnNames = columnInfos.Select(c => c.ColumnName).ToList();
				var referencedTableColumnNames = columnInfos.Select(c => c.GetTailColumnName());
				
				var referencesExpression = new SqlReferencesExpression
				(
					new SqlTableExpression(referencedTableName),
					SqlColumnReferenceDeferrability.InitiallyDeferred,
					referencedTableColumnNames, this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction).Value : (valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull)), this.FixAction((foreignObjectConstraintAttribute != null && this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnDeleteAction) != null) ? this.ToSqlColumnReferenceAction(foreignObjectConstraintAttribute.OnUpdateAction).Value : SqlColumnReferenceAction.NoAction)
				);
				
				var foreignKeyConstraint = new SqlConstraintExpression(referencesExpression, this.formatterManager.GetForeignKeyConstraintName(referencingProperty), currentTableColumnNames);

				this.currentTableConstraints.Add(foreignKeyConstraint);
			}
		}

		private SqlColumnDefinitionExpression BuildColumnDefinition(ColumnInfo columnInfo)
		{
			var sqlDataType = this.sqlDataTypeProvider.GetSqlDataType(columnInfo.DefinitionProperty.PropertyType);
			var columnDataTypeName = sqlDataType.GetSqlName(columnInfo.DefinitionProperty);
			var constraints = this.BuildColumnConstraints(columnInfo.DefinitionProperty, columnInfo.VisitedProperties.FirstOrDefault());

			return new SqlColumnDefinitionExpression(columnInfo.ColumnName, new SqlTypeExpression(columnDataTypeName, sqlDataType.IsUserDefinedType), constraints);
		}

		private IEnumerable<SqlColumnDefinitionExpression> BuildRelatedColumnDefinitions(TypeDescriptor typeDescriptor)
		{
			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos().Where(c => c.RelationshipType == RelationshipType.ChildOfOneToMany))
			{
				var foreignKeyColumns = QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, typeRelationshipInfo.ReferencingProperty);

				foreach (var result in this.BuildForeignKeyColumnDefinitions(typeRelationshipInfo.ReferencingProperty, foreignKeyColumns))
				{
					yield return result;
				}
			}
		}

		private Expression BuildCreateTableExpression(TypeDescriptor typeDescriptor)
		{
			var columnExpressions = new List<SqlColumnDefinitionExpression>();

			this.currentTableConstraints = new List<SqlConstraintExpression>();

			var columnInfos = QueryBinder.GetColumnInfos
			(
				this.model.TypeDescriptorProvider,
				typeDescriptor.PersistedProperties,
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
				typeDescriptor.PersistedPropertiesWithoutBackreferences.Where(c => !c.PropertyType.IsDataAccessObjectType()),
				(c, d) => d == 0 ? !c.IsPrimaryKey : c.IsPrimaryKey,
				(c, d) => d == 0 ? !c.IsPrimaryKey : c.IsPrimaryKey
			);

			foreach (var columnInfo in columnInfos)
			{
				columnExpressions.Add(this.BuildColumnDefinition(columnInfo));
			}

			foreach (var property in typeDescriptor.PersistedPropertiesWithoutBackreferences
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
				var columnNames = primaryKeys.Select(c => c.ColumnName);
				var compositePrimaryKeyConstraint = new SqlConstraintExpression(ConstraintType.PrimaryKey, this.formatterManager.GetPrimaryKeyConstraintName(typeDescriptor, typeDescriptor.PrimaryKeyProperties.ToArray()), columnNames.ToReadOnlyCollection());

				this.currentTableConstraints.Add(compositePrimaryKeyConstraint);
			}

			SqlOrganizationIndexExpression organizationIndex = null;

			var organizationAttributes = typeDescriptor
				.PersistedProperties
				.Where(c => c.OrganizationIndexAttribute != null)
				.Where(c => c.OrganizationIndexAttribute.IncludeOnly == false)
				.Select(c => new Tuple<OrganizationIndexAttribute, PropertyDescriptor>(c.OrganizationIndexAttribute, c))
				.OrderBy(c => c.Item1.CompositeOrder)
				.ToArray();
			
			if (organizationAttributes.Length > 0)
			{
				var organizationIndexName = this.formatterManager.GetIndexConstraintName(organizationAttributes.Select(c => c.Item2));

				organizationIndex = this.BuildOrganizationIndexExpression(organizationIndexName, organizationAttributes);
			}

			return new SqlCreateTableExpression(new SqlTableExpression(tableName), false, columnExpressions, this.currentTableConstraints, organizationIndex);
		}

		private SqlOrganizationIndexExpression BuildOrganizationIndexExpression(string indexName, Tuple<OrganizationIndexAttribute, PropertyDescriptor>[] properties)
		{
			var sorted = properties.OrderBy(c => c.Item1.CompositeOrder, Comparer<int>.Default);

			if (properties.Select(c => c.Item1).Any(c => c.Disable))
			{
				return new SqlOrganizationIndexExpression(indexName, null, null);
			}

			var indexedColumns = new List<SqlIndexedColumnExpression>();

			foreach (var attributeAndProperty in sorted.Where(c => !c.Item1.IncludeOnly))
			{
				foreach (var columnInfo in QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, attributeAndProperty.Item2))
				{
					indexedColumns.Add(new SqlIndexedColumnExpression(new SqlColumnExpression(columnInfo.DefinitionProperty.PropertyType, null, columnInfo.ColumnName), attributeAndProperty.Item1.SortOrder, attributeAndProperty.Item1.LowercaseIndex));
				}
			}

			var includedColumns = new List<SqlIndexedColumnExpression>();

			foreach (var attributeAndProperty in sorted.Where(c => c.Item1.IncludeOnly))
			{
				foreach (var columnInfo in QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, attributeAndProperty.Item2))
				{
					includedColumns.Add(new SqlIndexedColumnExpression(new SqlColumnExpression(columnInfo.DefinitionProperty.PropertyType, null, columnInfo.ColumnName)));
				}
			}

			return new SqlOrganizationIndexExpression(indexName, indexedColumns, includedColumns);
		}

		private Expression BuildIndexExpression(SqlTableExpression table, string indexName, Tuple<IndexAttribute, PropertyDescriptor>[] properties)
		{
			Expression where = null;
			var unique = properties.Select(c => c.Item1).Any(c => c.Unique);
			var lowercaseIndex = properties.Any(c => c.Item1.LowercaseIndex);
			var indexType = properties.Select(c => c.Item1.IndexType).FirstOrDefault(c => c != IndexType.Default);
			var sorted = properties.OrderBy(c => c.Item1.CompositeOrder, Comparer<int>.Default);

			var indexedColumns = new List<SqlIndexedColumnExpression>();

			foreach (var attributeAndProperty in sorted.Where(c => !c.Item1.IncludeOnly)) 
			{
				foreach (var columnInfo in QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, attributeAndProperty.Item2))
				{
					indexedColumns.Add(new SqlIndexedColumnExpression(new SqlColumnExpression(columnInfo.DefinitionProperty.PropertyType, null, columnInfo.ColumnName), attributeAndProperty.Item1.SortOrder, attributeAndProperty.Item1.LowercaseIndex));
				}
			}

			var includedColumns = new List<SqlIndexedColumnExpression>();

			foreach (var attributeAndProperty in sorted.Where(c => c.Item1.IncludeOnly))
			{
				foreach (var columnInfo in QueryBinder.GetColumnInfos(this.model.TypeDescriptorProvider, attributeAndProperty.Item2))
				{
					includedColumns.Add(new SqlIndexedColumnExpression(new SqlColumnExpression(columnInfo.DefinitionProperty.PropertyType, null, columnInfo.ColumnName)));
				}
			}

			Debug.Assert(properties.Select(c => c.Item2.PropertyInfo.DeclaringType).Distinct().Count() == 1);

			var parameterExpression = Expression.Parameter(properties.First().Item2.PropertyInfo.DeclaringType);

			foreach (var attributeAndProperty in sorted.Where(c => !c.Item1.Condition.IsNullOrEmpty()))
			{
				var expression = ComputedExpressionParser.Parse(attributeAndProperty.Item1.Condition, attributeAndProperty.Item2, parameterExpression, null, typeof(bool));

				if (expression == null)
				{
					continue;
				}

				where = where == null ? expression.Body : Expression.And(where, expression.Body);
			}

			if (where != null)
			{
				where = Expression.Lambda(where, parameterExpression);

				var call = Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(parameterExpression.Type), Expression.Constant(null, typeof(DataAccessObjects<>).MakeGenericType(parameterExpression.Type)), where);

				var projection = (SqlProjectionExpression)SqlQueryProvider.Optimize(this.dataAccessModel, SqlQueryProvider.Bind(this.dataAccessModel, this.sqlDataTypeProvider, call));

				where = projection.Select.Where;

				where = AliasReferenceReplacer.Replace(where, ((SqlTableExpression)projection.Select.From).Alias, null);
			}

			return new SqlCreateIndexExpression(indexName, table, unique, lowercaseIndex, indexType, false, indexedColumns, includedColumns, where);
		}

		private IEnumerable<Expression> BuildCreateIndexExpressions(TypeDescriptor typeDescriptor)
		{
			var allIndexAttributes = typeDescriptor.PersistedPropertiesWithoutBackreferences.Concat(typeDescriptor.RelationshipRelatedProperties).SelectMany(c => c.IndexAttributes.Select(d => new Tuple<IndexAttribute, PropertyDescriptor>(d, c)));

			var indexAttributesByName = allIndexAttributes.GroupBy(c => c.Item1.IndexName ?? typeDescriptor.PersistedName + "_" + c.Item2.PersistedName + "_idx").Sorted((x, y) => String.CompareOrdinal(x.Key, y.Key));

			var table = new SqlTableExpression(typeDescriptor.PersistedName);

			foreach (var group in indexAttributesByName)
			{
				var indexName = group.Key;

				var propertyDescriptors = group.ToArray();

				var index = this.BuildIndexExpression(table, indexName, propertyDescriptors);

				if (index != null)
				{
					yield return index;
				}
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

		public static Expression Build(DataAccessModel dataAccessModel, SqlQueryFormatterManager formatterManager, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, DataAccessModel model, DatabaseCreationOptions options, string tableNamePrefix, SqlDataDefinitionBuilderFlags flags)
		{
			var builder = new SqlDataDefinitionExpressionBuilder(dataAccessModel, formatterManager, sqlDialect, sqlDataTypeProvider, model, options, tableNamePrefix, flags);

			var retval = builder.Build();

			return retval;
		}
	}
}
