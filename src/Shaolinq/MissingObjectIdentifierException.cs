// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class MissingObjectIdentifierException
		: Exception
	{
		public IDataAccessObjectAdvanced DataAccessObject { get; }

		public MissingObjectIdentifierException(IDataAccessObjectAdvanced dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
