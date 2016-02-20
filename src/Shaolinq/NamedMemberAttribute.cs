// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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

		internal string GetName(PropertyDescriptor property, string transformString = "")
		{
			return VariableSubstituter.SedTransform(VariableSubstituter.Substitute(this.Name, property), transformString);
		}

		internal string GetPrefixName(PropertyDescriptor property, string transformString = "")
		{
			return VariableSubstituter.SedTransform(VariableSubstituter.Substitute(this.PrefixName, property), transformString);
		}

		internal string GetSuffixName(PropertyDescriptor property, string transformString = "")
		{
			return VariableSubstituter.SedTransform(VariableSubstituter.Substitute(this.SuffixName, property), transformString);
		}
	}
}