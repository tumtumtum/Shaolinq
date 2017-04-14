// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstraintExpression
		: SqlBaseExpression
	{
		public string ConstraintName { get; }
		public ConstraintType ConstraintType { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public SqlReferencesExpression ReferencesExpression { get; }
		public Expression DefaultValue { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Constraint;
		public object[] ConstraintOptions { get; }
		public bool NotNull => (this.ConstraintType & ConstraintType.NotNull) != 0;
		public bool AutoIncrement => (this.ConstraintType & ConstraintType.AutoIncrement) != 0;
		public bool Unique => (this.ConstraintType & ConstraintType.Unique) != 0;
		public bool PrimaryKey => (this.ConstraintType & ConstraintType.PrimaryKey) != 0;

		public SqlConstraintExpression(ConstraintType constraintType)
			: this(constraintType, (string)null)
		{
			this.ConstraintType = constraintType;
		}

		public SqlConstraintExpression(ConstraintType constraintType, string constraintName, object[] constraintOptions)
			: base(typeof(void))
		{
			this.ConstraintType = constraintType;
			this.ConstraintOptions = constraintOptions;
			this.ConstraintName = constraintName;
		}

		public SqlConstraintExpression(ConstraintType constraintType, string constraintName = null, IReadOnlyList<string> columnNames = null)
			: base(typeof(void))
		{
			this.ConstraintType = constraintType;
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
		
		public SqlConstraintExpression(ConstraintType constraintType, SqlReferencesExpression sqlReferencesExpression = null, string constraintName = null, IReadOnlyList<string> columnNames = null, Expression defaultValue = null, object[] constraintOptions = null)
			: base(typeof(void))
		{
			this.ConstraintType = constraintType;
			this.ConstraintName = constraintName;
			this.ReferencesExpression = sqlReferencesExpression;
			this.ColumnNames = columnNames;
			this.DefaultValue = defaultValue;
			this.ConstraintOptions = constraintOptions;
		}

		public SqlConstraintExpression ChangeColumnNames(IReadOnlyList<string> columnNames)
		{
			if (ReferenceEquals(columnNames, this.ColumnNames))
			{
				return this;
			}
		
			return new SqlConstraintExpression(this.ConstraintType, this.ReferencesExpression, this.ConstraintName, columnNames, this.DefaultValue);
		}

		public SqlConstraintExpression ChangeOptions(object[] options)
		{
			return new SqlConstraintExpression(this.ConstraintType, this.ReferencesExpression, this.ConstraintName, this.ColumnNames, this.DefaultValue, options);
		}

		public SqlConstraintExpression ChangeReferences(SqlReferencesExpression referencesExpression)
		{
			if (ReferenceEquals(this.ReferencesExpression, referencesExpression))
			{
				return this;
			}

			return new SqlConstraintExpression(this.ConstraintType, referencesExpression, this.ConstraintName, this.ColumnNames, this.DefaultValue);
		}

		public Expression ChangeConstraintName(string value)
		{
			return new SqlConstraintExpression(this.ConstraintType, this.ReferencesExpression, value, this.ColumnNames, this.DefaultValue);
		}
	}
}
