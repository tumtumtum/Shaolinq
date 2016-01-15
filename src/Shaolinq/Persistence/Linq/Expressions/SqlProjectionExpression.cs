// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlProjectionExpression
		: SqlBaseExpression
	{
		public Expression DefaultValue { get; }
		public bool IsElementTableProjection { get; }
		public SqlSelectExpression Select { get; }
		public Expression Projector { get; }
		public LambdaExpression Aggregator { get; }
		
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Projection;

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector)
			: this(select, projector, null)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, Expression defaultValue = null)
			: this(select.Type, select, projector, aggregator, false, defaultValue)
		{
		}
		
		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, Expression defaultValue = null)
			: this(select.Type, select, projector, aggregator, isElementTableProjection, defaultValue)
		{
		}

		public SqlProjectionExpression(Type type, SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, Expression defaultValue = null)
			: base(type)
		{
			this.Select = select;
			this.Projector = projector;
			this.Aggregator = aggregator;
			this.IsElementTableProjection = isElementTableProjection;
			this.DefaultValue = defaultValue;
		}

		public SqlProjectionExpression ToDefaultIfEmpty(Expression defaultValueExpression)
		{
			return new SqlProjectionExpression(this.Select.Type, this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, this.DefaultValue);
		}

		public SqlProjectionExpression ChangeType(Type type)
		{
			return new SqlProjectionExpression(type, this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, this.DefaultValue);
		}

		public SqlProjectionExpression ChangeAggregator(LambdaExpression aggregator)
		{
			return new SqlProjectionExpression(this.Type, this.Select, this.Projector, aggregator, this.IsElementTableProjection, this.DefaultValue);
		}

		public SqlProjectionExpression ChangeProjector(Expression projector)
		{
			return new SqlProjectionExpression(this.Type, this.Select, projector, this.Aggregator, this.IsElementTableProjection, this.DefaultValue);
		}
	}
}
