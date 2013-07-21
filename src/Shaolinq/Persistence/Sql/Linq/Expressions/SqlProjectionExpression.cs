using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlProjectionExpression
		: SqlBaseExpression
	{
		public bool IsDefaultIfEmpty { get; private set; }
		public bool IsElementTableProjection { get; private set; }
		public SqlSelectExpression Select { get; private set; }
		public Expression Projector { get; private set; }
		public LambdaExpression Aggregator { get; private set; }
		public SelectFirstType SelectFirstType { get; private set; }
		public Expression DefaultValueExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Projection;
			}
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator)
			: this(select, projector, aggregator, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection)
			: this(select, projector, aggregator, isElementTableProjection, SelectFirstType.None, null)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, SelectFirstType selectFirstType, Expression defaultValueExpression)
			: this(select, projector, aggregator, isElementTableProjection, selectFirstType, null, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, SelectFirstType selectFirstType, Expression defaultValueExpression, bool isDefaultIfEmpty)
			: base(select.Type)
		{
			this.Select = select;
			this.Projector = projector;
			this.Aggregator = aggregator;
			this.SelectFirstType = selectFirstType;
			this.DefaultValueExpression = defaultValueExpression;
			this.IsElementTableProjection = isElementTableProjection;
			this.IsDefaultIfEmpty = isDefaultIfEmpty;
		}

		public SqlProjectionExpression ToDefaultIfEmpty()
		{
			return new SqlProjectionExpression(this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, this.SelectFirstType, this.DefaultValueExpression, true);
		}
	}
}
