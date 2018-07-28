// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
			this.queue.Enqueue(expression);
		}

		public Expression Dequeue()
		{
			return this.queue.Count == 0 ? null : this.queue.Dequeue();
		}
	}
}