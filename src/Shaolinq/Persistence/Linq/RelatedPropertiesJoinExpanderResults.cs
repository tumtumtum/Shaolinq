// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Linq.Optimizers;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedPropertiesJoinExpanderResults
	{
		public Expression ProcessedExpression { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfos { get; set; }
		private readonly List<Tuple<Expression, Dictionary<ObjectPath<PropertyInfo>, Expression>>> replacementExpressionForPropertyPathsByJoin;

		internal RelatedPropertiesJoinExpanderResults(List<Tuple<Expression, Dictionary<ObjectPath<PropertyInfo>, Expression>>> replacementExpressionForPropertyPathsByJoin)
		{
			this.replacementExpressionForPropertyPathsByJoin = replacementExpressionForPropertyPathsByJoin;
		}

		public Expression GetReplacementExpression(Expression currentJoin, ObjectPath<PropertyInfo> propertyPath)
		{
			int index;
			var indexFound = -1;

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				Expression retval;

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Item1)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}
				
				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(propertyPath, out retval))
				{
					return retval;	
				}
			}

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				Expression retval;

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Item1)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(propertyPath, out retval))
				{
					return retval;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(ObjectPath<PropertyInfo>.Empty, out retval))
				{
					foreach (var property in propertyPath)
					{
						retval = Expression.Property(retval, property.Name);
					}

					return retval;
				}
			}

			throw new InvalidOperationException();
		}
	}
}