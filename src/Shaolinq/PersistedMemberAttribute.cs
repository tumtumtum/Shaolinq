using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PersistedMemberAttribute
		: Attribute
	{
		public string Name { get; set; }
		public string ShortName { get; set; }

		public string GetShortName(MemberInfo memberInfo)
		{
			return GetName(memberInfo, this.ShortName ?? this.Name);
		}

		public string GetName(MemberInfo memberInfo)
		{
			return GetName(memberInfo, this.Name);
		}

		private string GetName(MemberInfo memberInfo, string autoNamePattern)
		{
			if (autoNamePattern == null)
			{
				return memberInfo.Name;
			}

			return VariableSubstitutor.Substitute(autoNamePattern, (value) =>
			{
				switch (value)
				{
					case "$(TYPENAME)":
						return memberInfo.ReflectedType.Name;
					case "$(TYPENAME_LOWER)":
						return memberInfo.ReflectedType.Name.ToLower();
					case "$(PROPERTYNAME)":
						return memberInfo.Name;
					default:
						throw new NotSupportedException(value);
				}
			});
		}
	}
}
