// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlReferencesColumnExpression
		: SqlBaseExpression
	{
		public string ReferencedTableName {get;private set;}
		public ReadOnlyCollection<string> ReferencedColumnNames {get;private set;}
		public SqlColumnReferenceDeferrability Deferrability { get; private set; }
		public SqlColumnReferenceAction OnDeleteAction { get; private set; }
		public SqlColumnReferenceAction OnUpdateAction { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ReferencesColumn;
			}
		}

		public SqlReferencesColumnExpression(string referencedTableName, SqlColumnReferenceDeferrability deferrability, ReadOnlyCollection<string> referencedColumnNames, SqlColumnReferenceAction onDelete, SqlColumnReferenceAction onUpdate)
			: base(typeof(void))
		{
			this.OnDeleteAction = onDelete;
			this.OnUpdateAction = onUpdate;
			this.ReferencedTableName = referencedTableName;
			this.Deferrability = deferrability;
			this.ReferencedColumnNames = referencedColumnNames;
		}
	}
}
