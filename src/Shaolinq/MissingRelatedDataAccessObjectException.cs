// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class MissingRelatedDataAccessObjectException
		: MissingDataAccessObjectException
	{
		public IDataAccessObject ParentObject { get; private set; }

		public MissingRelatedDataAccessObjectException()
			: this(null, null)
		{
		}

		public MissingRelatedDataAccessObjectException(Exception innerException, string relatedQuery)
			: this(null, null, innerException, relatedQuery)
		{
		}

		public MissingRelatedDataAccessObjectException(IDataAccessObject missingObject)
			: this(missingObject, null, null, null)
		{
		}

		public MissingRelatedDataAccessObjectException(IDataAccessObject missingObject, IDataAccessObject parentObject, Exception innerException, string relatedQuery)
			: base(missingObject, innerException, relatedQuery)
		{
			this.ParentObject = parentObject;
		}
	}
}