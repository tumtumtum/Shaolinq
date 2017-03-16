// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
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

		public SqlConstraintExpression(SqlSimpleConstraint simpleConstraint, object[] constraintOptions = null)
			: base(typeof(void))
		{
			this.SimpleConstraint = simpleConstraint;
			this.ConstraintOptions = constraintOptions;
		}

		public SqlConstraintExpression(SqlSimpleConstraint simpleConstraint, IReadOnlyList<string> columnNames)
			: base(typeof(void))
		{
			this.SimpleConstraint = simpleConstraint;
			this.ColumnNames = columnNames;
		}

		public SqlConstraintExpression(string constraintName, SqlReferencesExpression sqlReferencesExpression, IReadOnlyList<string> columnNames = null, Expression defaultValue = null)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.ReferencesExpression = sqlReferencesExpression;
			this.ColumnNames = columnNames;
			this.DefaultValue = defaultValue;
		}

		public SqlConstraintExpression(string constraintName, SqlSimpleConstraint? simpleConstraint, SqlReferencesExpression sqlReferencesExpression, IReadOnlyList<string> columnNames = null, Expression defaultValue = null)
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

			return new SqlConstraintExpression(this.ConstraintName, this.SimpleConstraint, this.ReferencesExpression, columnNames, this.DefaultValue);
		}

		public SqlConstraintExpression ChangeReferences(SqlReferencesExpression referencesExpression)
		{
			if (ReferenceEquals(this.ReferencesExpression, referencesExpression))
			{
				return this;
			}

			return new SqlConstraintExpression(this.ConstraintName, this.SimpleConstraint, referencesExpression, this.ColumnNames, this.DefaultValue);
		}

		public SqlConstraintExpression ChangeSimpleConstraint(SqlSimpleConstraint? value)
		{
			if (value == this.SimpleConstraint)
			{
				return this;
			}

			return new SqlConstraintExpression(this.ConstraintName, value, this.ReferencesExpression, this.ColumnNames, this.DefaultValue);
		}
	}
}
