// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	/// <summary>
	/// Marks a propertyas one that references a collection of objects.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class DataAccessObjectsAttribute
		: Attribute
	{
	}
}
