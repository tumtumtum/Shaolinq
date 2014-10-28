// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public class WriteOnlyDataAccessObjectException
		: Exception
	{
		public IDataAccessObjectAdvanced DataAccessObject { get; private set; }

		public WriteOnlyDataAccessObjectException(IDataAccessObjectAdvanced dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
