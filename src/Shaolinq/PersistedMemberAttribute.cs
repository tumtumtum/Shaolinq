// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Reflection;
﻿using Shaolinq.Persistence;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PersistedMemberAttribute
		: Attribute
	{
		/// <summary>
		/// The persisted name of the property
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The short persisted name for this property. Short names are used as suffixes
		/// when naming related DataAccessObject properties.
		/// </summary>
		public string ShortName { get; set; }

		public string GetShortName(MemberInfo memberInfo, TypeDescriptor typeDescriptor)
		{
			return GetName(memberInfo, this.ShortName ?? this.Name, typeDescriptor);
		}

		public string GetName(MemberInfo memberInfo, TypeDescriptor typeDescriptor)
		{
			return GetName(memberInfo, this.Name, typeDescriptor);
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
					default:
						throw new NotSupportedException(value);
				}
			});
		}
	}
}
