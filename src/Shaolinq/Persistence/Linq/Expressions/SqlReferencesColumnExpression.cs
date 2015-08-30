// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlReferencesColumnExpression
		: SqlBaseExpression
	{
		public SqlTableExpression ReferencedTable {get; private set;}
		public IReadOnlyList<string> ReferencedColumnNames {get;private set;}
		public SqlColumnReferenceDeferrability Deferrability { get; private set; }
		public SqlColumnReferenceAction OnDeleteAction { get; private set; }
		public SqlColumnReferenceAction OnUpdateAction { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.ReferencesColumn; } }

		public SqlReferencesColumnExpression(SqlTableExpression referencedTable, SqlColumnReferenceDeferrability deferrability, IEnumerable<string> referencedColumnNames, SqlColumnReferenceAction onDelete, SqlColumnReferenceAction onUpdate)
			: this(referencedTable, deferrability, referencedColumnNames.ToReadOnlyList(), onDelete, onUpdate)
		{
		}

		public SqlReferencesColumnExpression(SqlTableExpression referencedTable, SqlColumnReferenceDeferrability deferrability, IReadOnlyList<string> referencedColumnNames, SqlColumnReferenceAction onDelete, SqlColumnReferenceAction onUpdate)
			: base(typeof(void))
		{
			this.OnDeleteAction = onDelete;
			this.OnUpdateAction = onUpdate;
			this.ReferencedTable = referencedTable;
			this.Deferrability = deferrability;
			this.ReferencedColumnNames = referencedColumnNames;
		}

		public SqlReferencesColumnExpression UpdateReferencedColumnNames(IEnumerable<string> columnNames)
		{
			return new SqlReferencesColumnExpression(this.ReferencedTable, this.Deferrability, columnNames, this.OnDeleteAction, this.OnUpdateAction);
		}
	}
}
