using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class SqlDataDefinitionExpressionBuilder
	{
		private readonly SqlDialect sqlDialect;
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private readonly DataAccessModel model;
		private readonly List<Expression> createTableExpressions;

		private SqlDataDefinitionExpressionBuilder(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, DataAccessModel model)
		{
			this.model = model; 
			this.sqlDialect = sqlDialect;
			this.sqlDataTypeProvider = sqlDataTypeProvider;

			this.createTableExpressions = new List<Expression>();
		}

		private List<Expression> BuildColumnConstraints(PropertyDescriptor propertyDescriptor, string[] columnNames, bool forForeignKey)
		{
			var retval = new List<Expression>();

			if (!propertyDescriptor.PropertyType.IsValueType || forForeignKey)
			{
				var valueRequiredAttribute = propertyDescriptor.ValueRequiredAttribute;

				if (valueRequiredAttribute != null && valueRequiredAttribute.Required)
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
				}
			}
			else
			{
				if (!propertyDescriptor.PropertyType.IsNullableType())
				{
					retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
				}
			}

			if (propertyDescriptor.IsPrimaryKey)
			{
				if (propertyDescriptor.PropertyType.IsIntegerType() && propertyDescriptor.IsAutoIncrement && !forForeignKey)
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
				retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.NotNull));
			}

			var defaultValueAttribute = propertyDescriptor.DefaultValueAttribute;

			if (defaultValueAttribute != null)
			{
				retval.Add(new SqlSimpleConstraintExpression(SqlSimpleConstraint.DefaultValue, null, defaultValueAttribute.Value));
			}

			return retval;
		}

		private IEnumerable<Expression> BuildColumnDefinitions(PropertyDescriptor propertyDescriptor, string columnName, bool asForeignKey)
		{
			if (columnName == null)
			{
				columnName = propertyDescriptor.PersistedName;
			}

			if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				foreach (var pair in this.sqlDialect.GetPersistedNames(this.model, propertyDescriptor))
				{
					yield return this.BuildColumnDefinitions(pair.Right, pair.Left, true).First();
				}

				yield break;
			}

			var sqlDataType = this.sqlDataTypeProvider.GetSqlDataType(propertyDescriptor.PropertyType);
			var columnDataTypeName = sqlDataType.GetSqlName(propertyDescriptor);
			var constraints = this.BuildColumnConstraints(propertyDescriptor, new[] { columnName }, false);

			yield return new SqlColumnDefinitionExpression(columnName, columnDataTypeName, constraints);
		}

		private List<Expression> BuildTableConstraints(TypeDescriptor typeDescriptor)
		{
			var retval = new List<Expression>();

			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				if (propertyDescriptor.PropertyType.IsDataAccessObjectType())
				{
					var names = this.sqlDialect.GetPersistedNames(this.model, propertyDescriptor).Select(c => c.Left).ToArray();

					var constraints = this.BuildColumnConstraints(propertyDescriptor, names, true);

					retval.AddRange(constraints);
				}
			}

			return retval;
		}

		private Expression BuildCreateTableExpression(TypeDescriptor typeDescriptor)
		{
			var columnExpressions = new List<Expression>();

			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties)
			{
				columnExpressions.AddRange(this.BuildColumnDefinitions(propertyDescriptor, propertyDescriptor.PersistedName, false));
			} 
			
			var tableConstraintExpressions = this.BuildTableConstraints(typeDescriptor);
			
			return new SqlCreateTableExpression(typeDescriptor.GetPersistedName(this.model), columnExpressions, tableConstraintExpressions);
		}

		private Expression Build()
		{
			foreach (var typeDescriptor in this.model.ModelTypeDescriptor.GetQueryableTypeDescriptors(this.model))
			{
				this.createTableExpressions.Add(BuildCreateTableExpression(typeDescriptor));
			}

			return new SqlStatementListExpression(this.createTableExpressions);
		}

		public static Expression Build(SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, DataAccessModel model)
		{
			var builder = new SqlDataDefinitionExpressionBuilder(sqlDialect, sqlDataTypeProvider, model);

			return builder.Build();
		}
	}
}
