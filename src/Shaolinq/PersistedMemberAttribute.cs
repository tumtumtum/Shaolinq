// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PersistedMemberAttribute
		: Attribute
	{
		public string Name { get; set; }
		public string ShortName { get; set; }
		public string PrefixName { get; set; }

		public string GetPrefixName(MemberInfo memberInfo, TypeDescriptor typeDescriptor)
		{
			return this.GetName(memberInfo, this.PrefixName ?? this.Name, typeDescriptor);
		}

        public string GetShortName(MemberInfo memberInfo, TypeDescriptor typeDescriptor)
		{
			return this.GetName(memberInfo, this.ShortName ?? this.Name, typeDescriptor);
		}

		public string GetName(MemberInfo memberInfo, TypeDescriptor typeDescriptor)
		{
			return this.GetName(memberInfo, this.Name, typeDescriptor);
		}

		private string GetName(MemberInfo memberInfo, string autoNamePattern, TypeDescriptor typeDescriptor)
		{
			if (autoNamePattern == null)
			{
				return memberInfo.Name;
			}
			
			return VariableSubstitutor.Substitute(autoNamePattern, (value) =>
			{
				switch (value)
				{
				case "$(PERSISTEDTYPENAME)":
					return typeDescriptor.PersistedName;
				case "$(PERSISTEDTYPENAME_LOWER)":
					return memberInfo.ReflectedType.Name.ToLower();
				case "$(TYPENAME)":
					return memberInfo.ReflectedType.Name;
				case "$(TYPENAME_LOWER)":
					return memberInfo.ReflectedType.Name.ToLower();
				case "$(PROPERTYNAME)":
					return memberInfo.Name;
				case "$(PROPERTYNAME_LOWER)":
					return memberInfo.Name.ToLower();
				default:
					throw new NotSupportedException(value);
				}
			});
		}
	}
}
