// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessObjectAttribute
		: DataAccessTypeAttribute
	{
		public bool NotPersisted { get; set; }
	}
}
