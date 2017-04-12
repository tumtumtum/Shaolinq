// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute
		: IndexAttributeBase
	{
		/// <summary>
		/// Specifies whether this propery should participate as part of the primary key.
		/// </summary>
		/// <remarks>
		/// Derived classes can reapply this attribute and set <c>IsPriamryKey</c> to <c>false</c>
		/// to opt the property out of being primary key.
		/// </remarks>
		public bool IsPrimaryKey { get; set; } = true;
	}
}
