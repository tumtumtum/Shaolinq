// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
﻿using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSelectExpression
		: SqlBaseExpression
	{
		public string Alias { get; }
		public bool Distinct { get; protected internal set; }
		public IReadOnlyList<SqlColumnDeclaration> Columns { get; }
		public Expression From { get; }
		public Expression Where { get; }
		public Expression Take { get; }
		public Expression Skip { get; }
		public bool ForUpdate { get; }
		public IReadOnlyList<Expression> OrderBy { get; }
		public IReadOnlyList<Expression> GroupBy { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Select;

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<Expression> orderBy, bool forUpdate)
			: this(type, alias, columns.ToReadOnlyList(), from, where, orderBy.ToReadOnlyList(), null, false, null, null, forUpdate)
		{
		}

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<Expression> orderBy, IEnumerable<Expression> groupBy, bool distinct, Expression skip, Expression take, bool forUpdate)
			: this(type, alias, columns.ToReadOnlyList(), from, where, orderBy.ToReadOnlyList(), groupBy.ToReadOnlyList(), distinct, skip, take, forUpdate)
		{	
		}

		public SqlSelectExpression(Type type, string alias, IReadOnlyList<SqlColumnDeclaration> columns, Expression from, Expression where, IReadOnlyList<Expression> orderBy, IReadOnlyList<Expression> groupBy, bool distinct, Expression skip, Expression take, bool forUpdate)
			: base(type)
		{
			this.Alias = alias;
			this.Distinct = distinct;
			this.Columns = columns;

			this.OrderBy = orderBy;
			this.GroupBy = groupBy;

			this.From = from;
			this.Where = where;
			this.Take = take;
			this.Skip = skip;
			this.ForUpdate = forUpdate;
		}
        
		public SqlSelectExpression ChangeColumns(IEnumerable<SqlColumnDeclaration> columns)
		{
			return ChangeColumns(columns, false);
		}

		public SqlSelectExpression ChangeColumns(IEnumerable<SqlColumnDeclaration> columns, bool columnsAlreadyOrdered)
		{
			return new SqlSelectExpression(this.Type, this.Alias, columnsAlreadyOrdered ? columns.ToReadOnlyList() : columns.OrderBy(c => c.Name).ToReadOnlyList(), this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, this.Take, this.Skip, this.ForUpdate);
		}

		public SqlSelectExpression ChangeWhere(Expression where)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, where, this.OrderBy, this.ForUpdate);
		}

		public SqlSelectExpression ChangeWhereAndColumns(Expression where, IReadOnlyList<SqlColumnDeclaration> columns, bool? forUpdate = null)
		{
			return new SqlSelectExpression(this.Type, this.Alias, columns, this.From, where, this.OrderBy, this.GroupBy, this.Distinct, this.Skip, this.Take, forUpdate ?? this.ForUpdate);
		}

		public SqlSelectExpression ChangeSkipTake(Expression skip, Expression take)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, skip, take, this.ForUpdate);
		}
	}
}
