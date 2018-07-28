// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// An attribute that declares that a property is a back reference to another object
	/// whereby the declaring object is a child in a one-to-many relationship with the other object.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class BackReferenceAttribute
		: PersistedMemberAttribute
	{
		public BackReferenceAttribute()
		{
		}

		public BackReferenceAttribute(string name)
			: base(name)
		{
		}
	}
}
