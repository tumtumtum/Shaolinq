// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstraintExpression
		: SqlBaseExpression
	{
		public string ConstraintName { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public SqlSimpleConstraint? SimpleConstraint { get; }
		public SqlReferencesExpression ReferencesExpression { get; }
		public Expression DefaultValue { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Constraint;
		public object[] ConstraintOptions { get; }

		public SqlConstraintExpression(SqlSimpleConstraint simpleConstraint, object[] constraintOptions = null, string constraintName = null)
			: base(typeof(void))
		{
			this.SimpleConstraint = simpleConstraint;
			this.ConstraintOptions = constraintOptions;
			this.ConstraintName = constraintName;
		}

		public SqlConstraintExpression(SqlSimpleConstraint simpleConstraint, IEnumerable<string> columnNames, string constraintName = null)
			: this(simpleConstraint, columnNames.ToReadOnlyCollection(), constraintName)
		{
		}

		public SqlConstraintExpression(SqlSimpleConstraint simpleConstraint, IReadOnlyList<string> columnNames, string constraintName = null)
			: base(typeof(void))
		{
			this.SimpleConstraint = simpleConstraint;
			this.ColumnNames = columnNames;
			this.ConstraintName = constraintName;
		}

		public SqlConstraintExpression(SqlReferencesExpression sqlReferencesExpression, string constraintName = null, IReadOnlyList<string> columnNames = null, Expression defaultValue = null)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.ReferencesExpression = sqlReferencesExpression;
			this.ColumnNames = columnNames;
			this.DefaultValue = defaultValue;
		}

		public SqlConstraintExpression(SqlSimpleConstraint? simpleConstraint, SqlReferencesExpression sqlReferencesExpression = null, string constraintName = null, IReadOnlyList<string> columnNames = null, Expression defaultValue = null)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.SimpleConstraint = simpleConstraint;
			this.ReferencesExpression = sqlReferencesExpression;
			this.ColumnNames = columnNames;
			this.DefaultValue = defaultValue;
		}

		public SqlConstraintExpression ChangeColumnNames(IReadOnlyList<string> columnNames)
		{
			if (ReferenceEquals(columnNames, this.ColumnNames))
			{
				return this;
			}

			return new SqlConstraintExpression(this.SimpleConstraint, this.ReferencesExpression, this.ConstraintName, columnNames, this.DefaultValue);
		}

		public SqlConstraintExpression ChangeReferences(SqlReferencesExpression referencesExpression)
		{
			if (ReferenceEquals(this.ReferencesExpression, referencesExpression))
			{
				return this;
			}

			return new SqlConstraintExpression(this.SimpleConstraint, referencesExpression, this.ConstraintName, this.ColumnNames, this.DefaultValue);
		}

		public SqlConstraintExpression ChangeSimpleConstraint(SqlSimpleConstraint? value)
		{
			if (value == this.SimpleConstraint)
			{
				return this;
			}

			return new SqlConstraintExpression(value, this.ReferencesExpression, this.ConstraintName, this.ColumnNames, this.DefaultValue);
		}
	}
}
