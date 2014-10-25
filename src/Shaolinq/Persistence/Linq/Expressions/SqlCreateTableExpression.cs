// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTableExpression
		: SqlBaseExpression
	{
		public bool IfNotExist { get; private set; }
		public SqlTableExpression Table { get; private set; }
		public ReadOnlyCollection<Expression> TableConstraints { get; private set; }
		public ReadOnlyCollection<Expression> ColumnDefinitionExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.CreateTable;
			}
		}

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IEnumerable<Expression> columnExpressions, IEnumerable<Expression> tableConstraintExpressions)
			: this(table, ifNotExist, columnExpressions.ToList(), tableConstraintExpressions.ToList())
		{
		}

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IList<Expression> columnExpressions, IList<Expression> tableConstraintExpressions)
			: this(table, ifNotExist, new ReadOnlyCollection<Expression>(columnExpressions), new ReadOnlyCollection<Expression>(tableConstraintExpressions))
		{
		}

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, ReadOnlyCollection<Expression> columnExpressions, ReadOnlyCollection<Expression> tableConstraintExpressions)
			: base(typeof(void))
		{
			this.Table = table;
			this.IfNotExist = ifNotExist;
			this.TableConstraints = tableConstraintExpressions;
			this.ColumnDefinitionExpressions = columnExpressions;
		}

		public SqlCreateTableExpression UpdateConstraints(IEnumerable<Expression> constraints)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, constraints);
		}
	}
}
