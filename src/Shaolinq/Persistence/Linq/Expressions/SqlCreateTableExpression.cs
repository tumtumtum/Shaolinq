// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTableExpression
		: SqlBaseExpression
	{
		public bool IfNotExist { get; private set; }
		public SqlTableExpression Table { get; private set; }
		public IReadOnlyList<Expression> TableConstraints { get; private set; }
		public IReadOnlyList<SqlColumnDefinitionExpression> ColumnDefinitionExpressions { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.CreateTable; } }

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IEnumerable<SqlColumnDefinitionExpression> columnExpressions, IEnumerable<Expression> tableConstraintExpressions)
			: this(table, ifNotExist, columnExpressions.ToReadOnlyList(), tableConstraintExpressions.ToReadOnlyList())
		{
		}

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IReadOnlyList<SqlColumnDefinitionExpression> columnExpressions, IReadOnlyList<Expression> tableConstraintExpressions)
			: base(typeof(void))
		{
			this.Table = table;
			this.IfNotExist = ifNotExist;
			this.TableConstraints = tableConstraintExpressions;
			this.ColumnDefinitionExpressions = columnExpressions;
		}

		public SqlCreateTableExpression UpdateConstraints(IReadOnlyList<Expression> constraints)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, constraints);
		}
	}
}
