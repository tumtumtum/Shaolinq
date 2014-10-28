// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public class MissingObjectIdentifierException
		: Exception
	{
		public IDataAccessObjectAdvanced DataAccessObject { get; private set; }

		public MissingObjectIdentifierException(IDataAccessObjectAdvanced dataAccessObject)
		{
			this.DataAccessObject = dataAccessObject;
		}
	}
}
