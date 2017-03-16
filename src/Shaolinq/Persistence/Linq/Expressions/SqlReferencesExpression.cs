// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlReferencesExpression
		: SqlBaseExpression
	{
		public SqlTableExpression ReferencedTable {get; }
		public SqlColumnReferenceDeferrability Deferrability { get; }
		public SqlColumnReferenceAction OnDeleteAction { get; }
		public SqlColumnReferenceAction OnUpdateAction { get; }
		public IReadOnlyList<string> ReferencedColumnNames { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.References;

		public SqlReferencesExpression(SqlTableExpression referencedTable, SqlColumnReferenceDeferrability deferrability, IEnumerable<string> referencedColumnNames, SqlColumnReferenceAction onDelete, SqlColumnReferenceAction onUpdate)
			: this(referencedTable, deferrability, referencedColumnNames.ToReadOnlyCollection(), onDelete, onUpdate)
		{
		}

		public SqlReferencesExpression(SqlTableExpression referencedTable, SqlColumnReferenceDeferrability deferrability, IReadOnlyList<string> referencedColumnNames, SqlColumnReferenceAction onDelete, SqlColumnReferenceAction onUpdate)
			: base(typeof(void))
		{
			this.OnDeleteAction = onDelete;
			this.OnUpdateAction = onUpdate;
			this.ReferencedTable = referencedTable;
			this.Deferrability = deferrability;
			this.ReferencedColumnNames = referencedColumnNames;
		}

		public SqlReferencesExpression ChangeDeferrability(SqlColumnReferenceDeferrability value)
		{
			if (this.Deferrability == value)
			{
				return this;
			}

			return new SqlReferencesExpression(this.ReferencedTable, value, this.ReferencedColumnNames, this.OnDeleteAction, this.OnUpdateAction);
		}

		public SqlReferencesExpression ChangeReferencedTable(SqlTableExpression value)
		{
			if (ReferenceEquals(this.ReferencedTable, value))
			{
				return this;
			}

			return new SqlReferencesExpression(value, this.Deferrability, this.ReferencedColumnNames, this.OnDeleteAction, this.OnUpdateAction);
		}

		public SqlReferencesExpression ChangeReferencedColumnNames(IEnumerable<string> columnNames)
		{
			if (ReferenceEquals(this.ReferencedColumnNames, columnNames))
			{
				return this;
			}

			return new SqlReferencesExpression(this.ReferencedTable, this.Deferrability, columnNames, this.OnDeleteAction, this.OnUpdateAction);
		}
	}
}
