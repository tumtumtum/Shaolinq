// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence
{
	public abstract class PersistenceStoreCreator
	{
		public abstract bool CreatePersistenceStorage(bool overwrite);
	}
}
