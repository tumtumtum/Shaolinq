// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlForeignKeyConstraintExpression
		: SqlBaseExpression
	{
		public string ConstraintName { get; set; }
		public ReadOnlyCollection<string> ColumnNames { get; set; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ForeignKeyConstraint;
			}
		}

		public SqlForeignKeyConstraintExpression(string constraintName, IEnumerable<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: this(constraintName, columnNames.ToList(), referencesColumnExpression)
		{	
		}

		public SqlForeignKeyConstraintExpression(string constraintName, IList<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: this(constraintName, new ReadOnlyCollection<string>(columnNames), referencesColumnExpression)
		{
		}

		public SqlForeignKeyConstraintExpression(string constraintName, ReadOnlyCollection<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.ColumnNames = columnNames;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}

		public SqlForeignKeyConstraintExpression UpdateColumnNamesAndReferencedColumnExpression(IEnumerable<string> columnNames, SqlReferencesColumnExpression sqlReferencesColumnExpression)
		{
			return new SqlForeignKeyConstraintExpression(this.ConstraintName, columnNames, sqlReferencesColumnExpression);
		}
	}
}
