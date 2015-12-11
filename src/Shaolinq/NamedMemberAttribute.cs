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

		internal string GetName(PropertyDescriptor property)
		{
			return VariableSubstituter.Substitute(this.Name, property);
		}

		internal string GetPrefixName(PropertyDescriptor property)
		{
			return VariableSubstituter.Substitute(this.PrefixName, property);
		}

		internal string GetSuffixName(PropertyDescriptor property)
		{
			return VariableSubstituter.Substitute(this.SuffixName, property);
		}
	}
}