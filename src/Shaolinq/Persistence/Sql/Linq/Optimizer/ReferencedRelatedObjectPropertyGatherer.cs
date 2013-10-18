using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private readonly BaseDataAccessModel model;
		private readonly ParameterExpression sourceParameterExpression;
		private readonly List<MemberExpression> results = new List<MemberExpression>();

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

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var level = CountLevel(memberExpression);

			if (level >= 2)
			{
				var add = true;
				var type = memberExpression.Expression.Type;
				var memberMemberExpression = memberExpression.Expression as MemberExpression;

				if (memberMemberExpression != null)
				{
					var typeDescriptor = this.model.GetTypeDescriptor(type);
					var propertyDescriptor = typeDescriptor.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name);

					if (propertyDescriptor.IsPrimaryKey || propertyDescriptor.IsReferencedObjectPrimaryKeyProperty)
					{
						add = false;
					}
				}

				if (add)
				{
					this.results.Add(memberExpression);
				}
			}

			return base.VisitMemberAccess(memberExpression);
		}
	}
}
