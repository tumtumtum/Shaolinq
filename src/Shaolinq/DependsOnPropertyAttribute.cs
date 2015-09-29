// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class DependsOnPropertyAttribute
		: Attribute
	{
		public string PropertyName { get; set; }

		public DependsOnPropertyAttribute(string propertyName)
		{
			this.PropertyName = propertyName;
		}
	}
}
