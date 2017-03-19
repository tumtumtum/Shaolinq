// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class MissingRelatedDataAccessObjectException
		: MissingDataAccessObjectException
	{
		public IDataAccessObjectAdvanced ParentObject { get; }

		public MissingRelatedDataAccessObjectException()
			: this(null, null)
		{
		}

		public MissingRelatedDataAccessObjectException(Exception innerException, string relatedQuery)
			: this(null, null, innerException, relatedQuery)
		{
		}

		public MissingRelatedDataAccessObjectException(IDataAccessObjectAdvanced missingObject)
			: this(missingObject, null, null, null)
		{
		}

		public MissingRelatedDataAccessObjectException(IDataAccessObjectAdvanced missingObject, IDataAccessObjectAdvanced parentObject, Exception innerException, string relatedQuery)
			: base(missingObject, innerException, relatedQuery)
		{
			this.ParentObject = parentObject;
		}
	}
}