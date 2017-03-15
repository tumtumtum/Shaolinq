// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlForeignKeyReferenceExpression
		: SqlBaseExpression
	{
		public string ConstraintName { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.ForeignKeyReference;

		public SqlForeignKeyReferenceExpression(string constraintName, IEnumerable<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: this(constraintName, columnNames.ToReadOnlyCollection(), referencesColumnExpression)
		{	
		}

		public SqlForeignKeyReferenceExpression(string constraintName, IReadOnlyList<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.ColumnNames = columnNames;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}

		public SqlForeignKeyReferenceExpression UpdateColumnNamesAndReferencedColumnExpression(IEnumerable<string> columnNames, SqlReferencesColumnExpression sqlReferencesColumnExpression)
		{
			return new SqlForeignKeyReferenceExpression(this.ConstraintName, columnNames, sqlReferencesColumnExpression);
		}
	}
}
