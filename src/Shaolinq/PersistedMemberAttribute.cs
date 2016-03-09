// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Specifies the attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class PersistedMemberAttribute
		: NamedMemberAttribute
	{
		public PersistedMemberAttribute()
		{
		}

		public PersistedMemberAttribute(string name)
			: base(name)
		{
		}
	}
}
