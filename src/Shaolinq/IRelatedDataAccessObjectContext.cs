// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public interface IRelatedDataAccessObjectContext
	{
		Type ElementType { get; }
		IDataAccessObjectAdvanced RelatedDataAccessObject { get; }
		LambdaExpression ExtraCondition { get; }
		Action<IDataAccessObjectAdvanced, IDataAccessObjectAdvanced> InitializeDataAccessObject { get; }
		EntityRelationshipType RelationshipType { get; }
	}
}
