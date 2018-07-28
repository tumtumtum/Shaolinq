// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
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
