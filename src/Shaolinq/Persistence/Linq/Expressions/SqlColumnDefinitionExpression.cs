// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlColumnDefinitionExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; private set; }
		public Expression ColumnType { get; private set; }
		public ReadOnlyCollection<Expression> ConstraintExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ColumnDefinition;
			}
		}

		public SqlColumnDefinitionExpression(string columnName, Expression columnTypeName, IEnumerable<Expression> constraintExpressions)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ColumnType = columnTypeName;
			this.ConstraintExpressions = new ReadOnlyCollection<Expression>(constraintExpressions.ToList());
		}

		public SqlColumnDefinitionExpression UpdateConstraints(IEnumerable<Expression> newConstraints)
		{
			return new SqlColumnDefinitionExpression(this.ColumnName, this.ColumnType, newConstraints);
		}
	}
}
