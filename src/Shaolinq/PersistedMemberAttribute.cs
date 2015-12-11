// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence;

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

			var memberIsDaoId = memberInfo.DeclaringType != null
								&& memberInfo.DeclaringType.IsGenericType
								&& memberInfo.DeclaringType.GetGenericTypeDefinition() == typeof(DataAccessObject<>)
								&& memberInfo.Name == "Id";

			return VariableSubstitutor.Substitute(autoNamePattern, (value) =>
			{
				switch (value)
				{
					case "$(PERSISTEDTYPENAME)":
						return memberIsDaoId ? "" : typeDescriptor.PersistedName;
					case "$(PERSISTEDTYPENAME_LOWER)":
						return memberIsDaoId ? "" : memberInfo.ReflectedType.Name.ToLower();
					case "$(TYPENAME)":
						return memberIsDaoId ? "" : memberInfo.ReflectedType.Name;
					case "$(TYPENAME_LOWER)":
						return memberIsDaoId ? "" : memberInfo.ReflectedType.Name.ToLower();
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
