// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq
{
	public interface IRelatedDataAccessObjectContext
	{
		Type ElementType { get; }
		LambdaExpression Condition { get; }
		IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
	}
}
