// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute
		: Attribute
	{
		/// <summary>
		/// The order of the index. Unspecified is database dependent but usually ascending.
		/// </summary>
		public SortOrder SortOrder { get; set; }

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		public int CompositeOrder { get; set; } = int.MinValue;

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
