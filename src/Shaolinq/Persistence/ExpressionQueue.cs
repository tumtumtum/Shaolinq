// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence
{
	public class ExpressionQueue
		: IExpressionQueue
	{
		private readonly Queue<Expression> queue = new Queue<Expression>();

		public void Enqueue(Expression expression)
		{
			queue.Enqueue(expression);
		}

		public Expression Dequeue()
		{
			if (this.queue.Count == 0)
			{
				return null;
			}

			return queue.Dequeue();
		}
	}
}