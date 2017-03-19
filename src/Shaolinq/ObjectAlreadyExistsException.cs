// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class ObjectAlreadyExistsException
		: UniqueConstraintException
	{
		public IDataAccessObjectAdvanced Object { get; }

		public ObjectAlreadyExistsException(IDataAccessObjectAdvanced obj, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.Object = obj;
		}
	}
}
