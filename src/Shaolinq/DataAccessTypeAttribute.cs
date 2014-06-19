// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessTypeAttribute
		: Attribute
	{
		public string Name { get; set; }

		public string GetName(Type type)
		{
			return this.Name ?? type.Name;
		}
	}
}
