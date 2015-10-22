// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute
		: Attribute
	{
		public bool IsPrimaryKey { get; set; }
		public int CompositeOrder { get; set; }

		public PrimaryKeyAttribute()
		{
			this.IsPrimaryKey = true;	
		}
	}
}
