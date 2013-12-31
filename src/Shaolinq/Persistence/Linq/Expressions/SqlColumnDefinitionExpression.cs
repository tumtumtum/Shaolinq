// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlColumnDefinitionExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; private set; }
		public string ColumnTypeName { get; private set; }
		public ReadOnlyCollection<Expression> ConstraintExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ColumnDefinition;
			}
		}

		public SqlColumnDefinitionExpression(string columnName, string columnTypeName, IList<Expression> constraintExpressions)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ColumnTypeName = columnTypeName;
			this.ConstraintExpressions = new ReadOnlyCollection<Expression>(constraintExpressions);
		}
	}
}
