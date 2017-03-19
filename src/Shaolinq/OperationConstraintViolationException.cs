// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class OperationConstraintViolationException
		: DataAccessException
	{
		public OperationConstraintViolationException()
		{
		}

		public OperationConstraintViolationException(string message, string relatedQuery)
			: base(message, relatedQuery)
		{
		}

		public OperationConstraintViolationException(Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
		}
	}
}
