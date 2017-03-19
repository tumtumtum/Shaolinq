// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlJoinExpression
		: SqlBaseExpression, ISqlExposesAliases
	{
		public SqlJoinType JoinType { get; set; }
		public Expression Left { get; }
		public Expression Right { get; }
		public Expression JoinCondition { get; }

		public string[] Aliases
		{
			get
			{
				var left = this.Left as ISqlExposesAliases;
				var right = this.Right as ISqlExposesAliases;

				if (left != null && right != null)
				{
					var newArray = new string[left.Aliases.Length + right.Aliases.Length];

					Array.Copy(left.Aliases, newArray, left.Aliases.Length);
					Array.Copy(right.Aliases, 0, newArray, left.Aliases.Length, right.Aliases.Length);

					return newArray;
				}
				else if (left != null)
				{
					return left.Aliases;
				}
				else if (right != null)
				{
					return right.Aliases;
				}
				else
				{
					return new string[0];
				}
			}
		}

		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Join;

		public SqlJoinExpression(Type type, SqlJoinType joinType, Expression left, Expression right, Expression joinCondition)
			: base(type)
		{
			if (joinType != SqlJoinType.OuterApply && joinType != SqlJoinType.CrossApply && joinType != SqlJoinType.Cross)
			{
				if (joinCondition == null)
				{
					throw new ArgumentNullException(nameof(joinCondition));
				}
			}

			this.JoinType = joinType; 
			this.Left = left;
			this.Right = right;
			this.JoinCondition = joinCondition;
		}
	}
}
