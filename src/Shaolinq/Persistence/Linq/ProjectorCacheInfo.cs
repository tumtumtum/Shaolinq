// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	internal struct ProjectorCacheInfo
	{
		public Delegate projector;
	    public Delegate asyncProjector;
	}

	internal struct ProjectorExpressionCacheInfo
	{
		public SqlQueryFormatResult formatResult;
		public SqlProjectionExpression projectionExpression;

		public ProjectorExpressionCacheInfo(SqlProjectionExpression projectionExpression, SqlQueryFormatResult formatResult)
			: this()
		{
			this.projectionExpression = projectionExpression;
			this.formatResult = formatResult;
		}
	}
}