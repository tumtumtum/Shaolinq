// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedPropertiesJoinExpanderResults
	{
		public Expression ProcessedExpression { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfos { get; set; }
		private readonly List<Pair<Expression, Dictionary<ObjectPath<PropertyInfo>, Expression>>> replacementExpressionForPropertyPathsByJoin;

		internal RelatedPropertiesJoinExpanderResults(List<Pair<Expression, Dictionary<ObjectPath<PropertyInfo>, Expression>>> replacementExpressionForPropertyPathsByJoin)
		{
			this.replacementExpressionForPropertyPathsByJoin = replacementExpressionForPropertyPathsByJoin;
		}

		public Expression GetReplacementExpression(Expression currentJoin, ObjectPath<PropertyInfo> propertyPath)
		{
			int index;
			var indexFound = -1;

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Left)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}
				
				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(propertyPath, out var retval))
				{
					return retval;	
				}
			}

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Left)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(propertyPath, out var retval))
				{
					return retval;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(ObjectPath<PropertyInfo>.Empty, out retval))
				{
					foreach (var property in propertyPath)
					{
						retval = Expression.Property(retval, property.Name);
					}

					return retval;
				}
			}

			return null;
		}
	}
}