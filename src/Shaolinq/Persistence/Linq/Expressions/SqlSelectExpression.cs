// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSelectExpression
		: SqlBaseExpression
	{
		public string Alias { get; private set; }
		public bool Distinct { get; protected internal set; }
		public ReadOnlyCollection<SqlColumnDeclaration> Columns { get; private set; }
		public Expression From { get; private set; }
		public Expression Where { get; private set; }
		public Expression Take { get; private set; }
		public Expression Skip { get; private set; }
		public bool ForUpdate { get; private set; }
		public ReadOnlyCollection<Expression> OrderBy { get; private set; }
		public ReadOnlyCollection<Expression> GroupBy { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Select;
			}
		}

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<SqlOrderByExpression> orderBy, bool forUpdate)
			: this(type, alias, columns, from, where, orderBy, null, false, null, null, forUpdate)
		{
		}

		public SqlSelectExpression(Type type, string alias, IEnumerable<SqlColumnDeclaration> columns, Expression from, Expression where, IEnumerable<Expression> orderBy, IEnumerable<Expression> groupBy, bool distinct, Expression skip, Expression take, bool forUpdate)
			: base(type)
		{
			this.Alias = alias;
			this.Distinct = distinct;
			this.Columns = columns as ReadOnlyCollection<SqlColumnDeclaration> ?? new List<SqlColumnDeclaration>(columns).AsReadOnly();

			this.OrderBy = orderBy as ReadOnlyCollection<Expression>;

			if (this.OrderBy == null && orderBy != null)
			{
				this.OrderBy = new List<Expression>(orderBy).AsReadOnly();
			}

			this.GroupBy = groupBy as ReadOnlyCollection<Expression>;

			if (this.GroupBy == null && groupBy != null)
			{
				this.GroupBy = new List<Expression>(groupBy).AsReadOnly();
			}

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
			return new SqlSelectExpression(this.Type, this.Alias, columnsAlreadyOrdered ? columns : columns.OrderBy(c => c.Name), this.From, this.Where, this.OrderBy, this.GroupBy, this.Distinct, this.Take, this.Skip, this.ForUpdate);
		}
	}
}
