// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public interface IRelatedDataAccessObjectContext
	{
		Type ElementType { get; }
		IDataAccessObject RelatedDataAccessObject { get; }
		LambdaExpression ExtraCondition { get; }
		Action<IDataAccessObject, IDataAccessObject> InitializeDataAccessObject { get; }
		EntityRelationshipType RelationshipType { get; }
	}
}
