// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private readonly List<Expression> createTableExpressions;
		private List<Expression> currentTableConstraints;

		private SqlDataDefinitionExpressionBuilder(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, DataAccessModel model)
		{
			this.model = model; 
			this.sqlDialect = sqlDialect;
			this.sqlDataTypeProvider = sqlDataTypeProvider;

			this.createTableExpressions = new List<Expression>();
			this.currentTableConstraints = new List<Expression>();
		}

		private List<Expression> BuildColumnConstraints(PropertyDescriptor propertyDescriptor, string[] columnNames, PropertyDescriptor foreignKeyReferencingProperty)
		{
			var retval = new List<Expression>();

			if (foreignKeyReferencingProperty != null)
			{
				var valueRequiredAttribute = foreignKeyReferencingProperty.ValueRequiredAttribute;

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

				if (propertyDescriptor.IsPrimaryKey)
				{
					if (propertyDescriptor.PropertyType.IsIntegerType() && propertyDescriptor.IsAutoIncrement)
					{
						retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKeyAutoIncrement));
					}
					else
					{
						retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.PrimaryKey));
					}
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

		private IEnumerable<Expression> BuildForeignKeyColumnDefinitions(PropertyDescriptor referencingProperty, ForeignKeyColumnInfo[] columnNamesAndReferencedTypeProperties)
		{
			var relatedPropertyTypeDescriptor = this.model.ModelTypeDescriptor.GetQueryableTypeDescriptor(referencingProperty.PropertyType);
			var referencedTableName = relatedPropertyTypeDescriptor.GetPersistedName(this.model);

			var valueRequired = (referencingProperty.ValueRequiredAttribute != null && referencingProperty.ValueRequiredAttribute.Required);
			var supportsInlineForeignKeys = this.sqlDialect.SupportsFeature(SqlFeature.SupportsAndPrefersInlineForeignKeysWherePossible);

			foreach (var foreignKeyColumn in columnNamesAndReferencedTypeProperties)
			{
				var retval = (SqlColumnDefinitionExpression)this.BuildColumnDefinitions(foreignKeyColumn.KeyPropertyOnForeignType, foreignKeyColumn.ColumnName, referencingProperty).First();

				if (columnNamesAndReferencedTypeProperties.Length == 1 && supportsInlineForeignKeys)
				{
					var names = new ReadOnlyCollection<string>(new[] { foreignKeyColumn.KeyPropertyOnForeignType.PersistedName });
					var newConstraints = new List<Expression>(retval.ConstraintExpressions);
					var referencesColumnExpression = new SqlReferencesColumnExpression(referencedTableName, SqlColumnReferenceDeferrability.InitiallyDeferred, names, valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull, SqlColumnReferenceAction.NoAction);

					newConstraints.Add(referencesColumnExpression);

					retval = new SqlColumnDefinitionExpression(retval.ColumnName, retval.ColumnTypeName, newConstraints);
				}

				yield return retval;
			}

			if (columnNamesAndReferencedTypeProperties.Length > 1 || !supportsInlineForeignKeys)
			{
				var currentTableColumnNames = new ReadOnlyCollection<string>(columnNamesAndReferencedTypeProperties.Select(c => c.ColumnName).ToList());
				var referencedTableColumnNames = new ReadOnlyCollection<string>(columnNamesAndReferencedTypeProperties.Select(c => c.KeyPropertyOnForeignType.PersistedName).ToList());
				var referencesColumnExpression = new SqlReferencesColumnExpression(referencedTableName, SqlColumnReferenceDeferrability.InitiallyDeferred, referencedTableColumnNames, valueRequired ? SqlColumnReferenceAction.Restrict : SqlColumnReferenceAction.SetNull, SqlColumnReferenceAction.NoAction);
				var foreignKeyConstraint = new SqlForeignKeyConstraintExpression(null, currentTableColumnNames, referencesColumnExpression);

				currentTableConstraints.Add(foreignKeyConstraint);
			}
		}

		private IEnumerable<Expression> BuildColumnDefinitions(PropertyDescriptor propertyDescriptor, string columnName, PropertyDescriptor foreignKeyReferencingProperty)
		{
			if (columnName == null)
			{
				columnName = propertyDescriptor.PersistedName;
			}

			if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				var foreignKeyColumn = QueryBinder.ExpandPropertyIntoForeignKeyColumns(this.model, propertyDescriptor);
				
				foreach (var result in this.BuildForeignKeyColumnDefinitions(propertyDescriptor, foreignKeyColumn))
				{
					yield return result;
				}

				yield break;
			}

			var sqlDataType = this.sqlDataTypeProvider.GetSqlDataType(propertyDescriptor.PropertyType);
			var columnDataTypeName = sqlDataType.GetSqlName(propertyDescriptor);
			var constraints = this.BuildColumnConstraints(propertyDescriptor, new[] { columnName }, foreignKeyReferencingProperty);

			yield return new SqlColumnDefinitionExpression(columnName, columnDataTypeName, constraints);
		}

		private IEnumerable<Expression> BuildRelatedColumnDefinitions(TypeDescriptor typeDescriptor)
		{
			foreach (var typeRelationshipInfo in typeDescriptor.GetRelationshipInfos())
			{
				if (typeRelationshipInfo.EntityRelationshipType == EntityRelationshipType.ChildOfOneToMany)
				{
					var relatedPropertyTypeDescriptor = this.model.ModelTypeDescriptor.GetQueryableTypeDescriptor(typeRelationshipInfo.ReferencingProperty.PropertyType);
					var referencedTableName = relatedPropertyTypeDescriptor.GetPersistedName(this.model);
					var foreignKeyColumns = QueryBinder.ExpandPropertyIntoForeignKeyColumns(this.model, relatedPropertyTypeDescriptor, referencedTableName);

					foreach (var result in this.BuildForeignKeyColumnDefinitions(typeRelationshipInfo.ReferencingProperty, foreignKeyColumns))
					{
						yield return result;
					}
				}
			}
		}

		private Expression BuildCreateTableExpression(TypeDescriptor typeDescriptor)
		{
			var columnExpressions = new List<Expression>();

			currentTableConstraints = new List<Expression>();

			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				columnExpressions.AddRange(this.BuildColumnDefinitions(propertyDescriptor, propertyDescriptor.PersistedName, null));
			}

			columnExpressions.AddRange(BuildRelatedColumnDefinitions(typeDescriptor));
			
			return new SqlCreateTableExpression(typeDescriptor.GetPersistedName(this.model), columnExpressions, currentTableConstraints);
		}

		private Expression Build()
		{
			foreach (var typeDescriptor in this.model.ModelTypeDescriptor.GetQueryableTypeDescriptors(this.model))
			{
				this.createTableExpressions.Add(BuildCreateTableExpression(typeDescriptor));
			}

			return new SqlStatementListExpression(new ReadOnlyCollection<Expression>(this.createTableExpressions));
		}

		public static Expression Build(SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, DataAccessModel model)
		{
			var builder = new SqlDataDefinitionExpressionBuilder(sqlDialect, sqlDataTypeProvider, model);

			var retval = builder.Build();

			retval = SqlMultiColumnPrimaryKeyCoalescer.Coalesce(retval);

			return retval;
		}
	}
}
