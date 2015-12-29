// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlProjectionExpression
		: SqlBaseExpression
	{
		public bool IsDefaultIfEmpty { get; }
		public bool IsElementTableProjection { get; }
		public SqlSelectExpression Select { get; }
		public Expression Projector { get; }
		public LambdaExpression Aggregator { get; }
		public Expression DefaultValueExpression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Projection;

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector)
			: this(select, projector, null)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator)
			: this(select, projector, aggregator, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection)
			: this(select, projector, aggregator, isElementTableProjection, null, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, Expression defaultValueExpression, bool isDefaultIfEmpty)
			: this(select.Type, select, projector, aggregator, isElementTableProjection, null, false)
		{
		}

		public SqlProjectionExpression(Type type, SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, Expression defaultValueExpression, bool isDefaultIfEmpty)
			: base(type)
		{
			this.Select = select;
			this.Projector = projector;
			this.Aggregator = aggregator;
			this.DefaultValueExpression = defaultValueExpression;
			this.IsElementTableProjection = isElementTableProjection;
			this.IsDefaultIfEmpty = isDefaultIfEmpty;
		}

		public SqlProjectionExpression ToDefaultIfEmpty(Expression defaultValueExpression)
		{
			return new SqlProjectionExpression(this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, defaultValueExpression, true);
		}

		public SqlProjectionExpression ChangeType(Type type)
		{
			return new SqlProjectionExpression(type, this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, this.DefaultValueExpression, this.IsDefaultIfEmpty);
		}
	}
}
