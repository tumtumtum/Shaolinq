// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SubstituteConstantsResult
	{
		public Expression Body { get; }
		public ParameterExpression[] AdditionalParameters { get; }
		internal volatile Delegate compiledSimpleVersion;
		
		public SubstituteConstantsResult(Expression body, ParameterExpression[] additionalParameters)
		{
			this.Body = body;
			this.AdditionalParameters = additionalParameters;
		}
	}
}