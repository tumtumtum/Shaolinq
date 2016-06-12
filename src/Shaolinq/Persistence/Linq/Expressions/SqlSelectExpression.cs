// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSelectExpression
		: SqlAliasedExpression
	{
		public bool Distinct { get; }
		public Expression From { get; }
		public Expression Where { get; }
		public Expression Take { get; }
		public Expression Skip { get; }
		public bool ForUpdate { get; }
		public bool Reverse { get; set; }
		public IReadOnlyList<Expression> OrderBy { get; }
		public IReadOnlyList<Expression> GroupBy { get; }
		public IReadOnlyList<SqlColumnDeclaration> Columns { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Select;

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<Expression> orderBy, bool forUpdate = false, bool reverse = false)
			: this(type, alias, columns.ToReadOnlyCollection(), from, where, orderBy.ToReadOnlyCollection(), null, false, null, null, forUpdate, reverse)
		{
		}

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<Expression> orderBy, IEnumerable<Expression> groupBy, bool distinct, Expression skip, Expression take, bool forUpdate = false, bool reverse = false)
			: this(type, alias, columns.ToReadOnlyCollection(), from, where, orderBy.ToReadOnlyCollection(), groupBy.ToReadOnlyCollection(), distinct, skip, take, forUpdate, reverse)
		{	
		}

		public SqlSelectExpression(Type type, string alias, IReadOnlyList<SqlColumnDeclaration> columns, Expression from, Expression where, IReadOnlyList<Expression> orderBy, IReadOnlyList<Expression> groupBy, bool distinct, Expression skip, Expression take, bool forUpdate = false, bool reverse = false)
			: base(type, alias)
		{
			this.Distinct = distinct;
			this.Columns = columns;

			this.OrderBy = orderBy;
			this.GroupBy = groupBy;

			this.From = from;
			this.Where = where;
			this.Take = take;
			this.Skip = skip;
			this.ForUpdate = forUpdate;
			this.Reverse = reverse;
		}
		
		public SqlSelectExpression ChangeColumns(IEnumerable<SqlColumnDeclaration> columns)
		{
			return this.ChangeColumns(columns, false);
		}

		public SqlSelectExpression ChangeColumns(IEnumerable<SqlColumnDeclaration> columns, bool columnsAlreadyOrdered)
		{
			return new SqlSelectExpression(this.Type, this.Alias, columnsAlreadyOrdered ? columns.ToReadOnlyCollection() : columns.OrderBy(c => c.Name).ToReadOnlyCollection(), this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, this.Take, this.Skip, this.ForUpdate);
		}

		public SqlSelectExpression ChangeAlias(string alias)
		{
			return new SqlSelectExpression(this.Type, alias, this.Columns, this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, this.Take, this.Skip, this.ForUpdate);
		}

		public SqlSelectExpression ChangeWhere(Expression where)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, where, this.OrderBy, this.GroupBy, this.Distinct, this.Skip, this.Take, this.ForUpdate, this.Reverse);
		}

		public SqlSelectExpression ChangeWhereAndColumns(Expression where, IReadOnlyList<SqlColumnDeclaration> columns, bool? forUpdate = null)
		{
			return new SqlSelectExpression(this.Type, this.Alias, columns, this.From, where, this.OrderBy, this.GroupBy, this.Distinct, this.Skip, this.Take, forUpdate ?? this.ForUpdate, this.Reverse);
		}

		public SqlSelectExpression ChangeSkipTake(Expression skip, Expression take)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, skip, take, this.ForUpdate, this.Reverse);
		}

		public SqlSelectExpression ChangeOrderBy(IEnumerable<Expression> orderBy)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, this.Where, orderBy, this.GroupBy, this.Distinct, this.Skip, this.Take, this.ForUpdate, this.Reverse);
		}

		public SqlSelectExpression ChangeForUpdate(bool forUpdate)
		{
			return new SqlSelectExpression(this.Type, this.Alias, this.Columns, this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, this.Skip, this.Take, forUpdate, this.Reverse);
		}
	}
}
