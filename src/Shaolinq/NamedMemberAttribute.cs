// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class NamedMemberAttribute
		: Attribute
	{
		/// <summary>
		/// The column name for the current member
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The name used when constructing the foriegn key column name amd the current member is the last part of a nested primary key.
		/// </summary>
		/// <remarks>
		/// By default this value will be set to the property name.
		/// Refer to the documentation for <see cref="PrefixName"/> to see how <see cref="PrefixName"/> and <see cref="SuffixName"/> are used.
		/// </remarks>
		/// <seealso cref="PrefixName"/>
		public string SuffixName { get; set; }

		/// <summary>
		/// The name used when constructing the foriegn key column name amd the current member is the non-last part of a nested primary key.
		/// </summary>
		/// <remarks>
		/// By default this value will be set to the property name.
		/// Nested primary keys occur when a <see cref="DataAccessObject"/> (<c>Object1</c>) defines a primary key that is itself a <see cref="DataAccessObject"/> (<c>KeyObject</c>).
		/// When the <c>Object1</c> is subsequently referenced by another <see cref="DataAccessObject"/> (<c>Object2.Property</c>) then the column name for the foriegn key on <c>Object2</c>
		/// would be <c>Object2.Property.Name + Object1.Id.PrefixName + KeyObject.Id.SuffixName</c>.
		/// </remarks>
		/// <see cref="SuffixName"/>
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