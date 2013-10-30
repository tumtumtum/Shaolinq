using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private bool disableCompare;
		private readonly BaseDataAccessModel model;
		private readonly ParameterExpression sourceParameterExpression;
		private readonly List<MemberExpression> results = new List<MemberExpression>();
		private readonly HashSet<Expression> expressionsToIgnore = new HashSet<Expression>();

		private class DisableCompareContext
			: IDisposable
		{
			private readonly bool savedDisableCompare;
			private readonly ReferencedRelatedObjectPropertyGatherer gatherer;

			public DisableCompareContext(ReferencedRelatedObjectPropertyGatherer gatherer)
			{
				this.gatherer = gatherer;
				savedDisableCompare = gatherer.disableCompare;
				gatherer.disableCompare = true;
			}

			public void Dispose()
			{
				this.gatherer.disableCompare = savedDisableCompare;
			}
		}

		protected IDisposable AcquireDisableCompareContext()
		{
			return new DisableCompareContext(this);
		}
		
		public ReferencedRelatedObjectPropertyGatherer(BaseDataAccessModel model, ParameterExpression sourceParameterExpression)
		{
			this.model = model;
			this.sourceParameterExpression = sourceParameterExpression;
		}

		public static List<MemberExpression> Gather(BaseDataAccessModel model, Expression expression, ParameterExpression sourceParameterExpression)
		{
			var gatherer = new ReferencedRelatedObjectPropertyGatherer(model, sourceParameterExpression);
			
			gatherer.Visit(expression);

			return gatherer.results;
		}

		private static int CountLevel(MemberExpression memberExpression)
		{
			if (!memberExpression.Expression.Type.IsDataAccessObjectType())
			{
				return 0;
			}

			if (!(memberExpression.Expression is MemberExpression))
			{
				return 1;
			}

			return 1 + CountLevel(((MemberExpression)memberExpression.Expression));
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			using (this.AcquireDisableCompareContext())
			{
				return base.VisitBinary(binaryExpression);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			Expression test;

			using (this.AcquireDisableCompareContext())
			{
				test = this.Visit(expression.Test);
			}

			var ifTrue = this.Visit(expression.IfTrue);
			var ifFalse = this.Visit(expression.IfFalse);

			if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			{
				return Expression.Condition(test, ifTrue, ifFalse);
			}
			else
			{
				return expression;
			}
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var level = CountLevel(memberExpression);
			var max = memberExpression.Type.IsDataAccessObjectType() ? 1 : 2;

			if (level >= max && !expressionsToIgnore.Contains(memberExpression))
			{
				var add = false;
				var type = memberExpression.Expression.Type;
				var memberMemberExpression = memberExpression.Expression as MemberExpression;

				if (memberMemberExpression != null)
				{
					add = true;

					var typeDescriptor = this.model.GetTypeDescriptor(type);
					var propertyDescriptor = typeDescriptor.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name);

					if (propertyDescriptor.IsPrimaryKey || propertyDescriptor.IsReferencedObjectPrimaryKeyProperty)
					{
						add = false;
					}

					if (sourceParameterExpression != null)
					{
						if (memberMemberExpression.Expression != sourceParameterExpression)
						{
							add = false;
						}
					}
				}
				else if (memberExpression.Type.IsDataAccessObjectType() && !disableCompare)
				{
					add = true;
				}

				if (add)
				{
					this.results.Add(memberExpression);
				
					var expression = memberExpression;

					while (expression != null)
					{
						expressionsToIgnore.Add(expression);
						expression = expression.Expression as MemberExpression;
					}
				}
			}

			return base.VisitMemberAccess(memberExpression);
		}
	}
}
