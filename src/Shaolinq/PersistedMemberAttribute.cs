using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PersistedMemberAttribute
		: Attribute
	{
		private static readonly Regex Regex = new Regex(@"\$\([a-zA-Z]+\)", RegexOptions.Compiled); 
		
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

			var s = Regex.Replace(autoNamePattern, match =>
            {
				switch (match.Groups[0].Value)
                {
                	case "$(TYPENAME)":
						return memberInfo.ReflectedType.Name;
					case "$(PROPERTYNAME)":
						return memberInfo.Name;
					default:
                		throw new NotSupportedException(match.Groups[0].Value);
                }
            });

			return s;
		}
	}
}
