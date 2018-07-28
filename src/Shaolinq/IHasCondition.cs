// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq
{
	public interface IHasCondition
	{
		LambdaExpression Condition { get; }
	}
}