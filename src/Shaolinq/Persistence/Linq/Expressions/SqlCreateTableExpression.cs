// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTableExpression
		: SqlBaseExpression
	{
		public Expression Table { get; set; }
		public ReadOnlyCollection<Expression> TableConstraints { get; private set; }
		public ReadOnlyCollection<Expression> ColumnDefinitionExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.CreateTable;
			}
		}

		public SqlCreateTableExpression(Expression table, IList<Expression> columnExpressions, IList<Expression> tableConstraintExpressions)
			: base(typeof(void))
		{
			this.Table = table;
			this.TableConstraints = new ReadOnlyCollection<Expression>(tableConstraintExpressions);
			this.ColumnDefinitionExpressions = new ReadOnlyCollection<Expression>(columnExpressions);
		}
	}
}
