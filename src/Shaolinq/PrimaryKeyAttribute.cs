// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute
		: Attribute
	{
		public bool IsPrimaryKey { get; set; }

		public PrimaryKeyAttribute()
		{
			this.IsPrimaryKey = true;	
		}
	}
}
