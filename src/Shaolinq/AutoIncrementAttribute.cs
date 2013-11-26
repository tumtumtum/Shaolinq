// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	/// <summary>
	/// Applied to a persistable property to indicate that each new object will automatically be assigned a value.
	/// Only applicable to integer and Guid properties.
	/// </summary>
	/// <remarks>
	/// This property can also be applied on overridden properties to disable the autoincrement attribute by setting the <see cref="AutoIncrement"/> property to false.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property)]
	public class AutoIncrementAttribute
		: Attribute
	{
		public bool AutoIncrement { get; set; }

		public AutoIncrementAttribute()
			: this(true)    
		{
		}

		public AutoIncrementAttribute(bool autoIncrement)
		{
			this.AutoIncrement = autoIncrement;
		}
	}
}
