// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence
{
	public interface IExpressionQueue
	{
		void Enqueue(Expression expression);
		Expression Dequeue();
	}
}