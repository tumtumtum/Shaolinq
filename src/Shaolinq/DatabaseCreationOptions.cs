// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public enum DatabaseCreationOptions
	{
		[Obsolete] IfNotExist,
		IfDatabaseNotExist = 0,
		[Obsolete] DeleteExisting = 1,
		DeleteExistingDatabase = 1,
	}
}
