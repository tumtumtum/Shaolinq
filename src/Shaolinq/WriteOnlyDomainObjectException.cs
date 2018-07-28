// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class WriteOnlyDataAccessObjectException
		: Exception
	{
		public IDataAccessObjectAdvanced DataAccessObject { get; }

		public WriteOnlyDataAccessObjectException(IDataAccessObjectAdvanced dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
