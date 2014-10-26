// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq
{
	public class DeletedDataAccessObjectException
		: MissingDataAccessObjectException
	{
		public DeletedDataAccessObjectException(IDataAccessObject dataAccessObject)
			: base(dataAccessObject)
		{	
		}
	}
}
