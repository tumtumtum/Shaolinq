// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class NamedMemberAttribute
		: Attribute
	{
		public string Name { get; set; }
		public string SuffixName { get; set; }
		public string PrefixName { get; set; }

		protected NamedMemberAttribute()
		{	
		}

		protected NamedMemberAttribute(string name)
		{
			this.Name = name;
		}
	}
}