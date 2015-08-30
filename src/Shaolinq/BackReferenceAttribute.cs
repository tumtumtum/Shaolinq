// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Reflection;
﻿using Shaolinq.Persistence;

namespace Shaolinq
{
	/// <summary>
	/// An attribute that declares that a property is a back reference to another object
	/// whereby the declaring object is a child in a one-to-many relationship with the other object.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class BackReferenceAttribute
		: Attribute
	{
		public string Name { get; private set; }

		public BackReferenceAttribute()
		{
		}

		public BackReferenceAttribute(string name)
		{
			this.Name = name;
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
