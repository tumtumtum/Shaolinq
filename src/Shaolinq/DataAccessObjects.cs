// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq
{
	public class DataAccessObjects<T>
		: DataAccessObjectsQueryable<T>
		where T : class, IDataAccessObject
	{
	}
}
