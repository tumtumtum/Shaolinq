// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessObjectAttribute
		: DataAccessTypeAttribute
	{
		public DataAccessObjectAttribute()
		{	
		}

		public DataAccessObjectAttribute(string name)
			: base(name)
		{
		}

		public bool NotPersisted { get; set; }
	}
}
