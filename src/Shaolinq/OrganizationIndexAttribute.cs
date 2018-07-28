// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
	public class OrganizationIndexAttribute
		: Attribute
	{
		/// <summary>
		/// The order of the index. Unspecified is database dependent but usually ascending.
		/// </summary>
		/// <remarks>
		/// This property is ignored if the attribute is applied at the class level.
		/// </remarks>
		public SortOrder SortOrder { get; set; }

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		/// <remarks>
		/// This property is ignored if the attribute is applied at the class level.
		/// </remarks>
		public int CompositeOrder { get; set; } = int.MinValue;

		/// <summary>
		/// Set to true on a primary key or class to remove the default organization index and use heap storage instead (MSSQL only)
		/// </summary>
		public bool Disable { get; set; } 
		
		/// <summary>
		/// If supported, the value of strings will be lower cased before creating the index
		/// </summary>
		/// <remarks>
		/// This property is ignored if the attribute is applied at the class level.
		/// </remarks>
		public bool Lowercase { get; set; }

		/// <summary>
		/// If supported, the value of strings will be lower cased before creating the index
		/// </summary>
		/// <remarks>
		/// This property is ignored if the attribute is applied at the class level.
		/// </remarks>
		[Obsolete("Use Lowercase instead")]
		public bool LowercaseIndex { get => this.Lowercase; set => this.Lowercase = value; }

		/// <summary>
		/// The name of the index. Some databases require index names to be server-unique rather than
		/// database-unique. Use the same <c>IndexName</c> across multiple properties to create
		/// a composite index
		/// </summary>
		public string IndexName { get; set; }

		/// <summary>
		/// The index should be a unique index
		/// </summary>
		public bool Unique { get; set; }

		/// <summary>
		/// The properties that make up the index (only applicable if attribute is used on a class)
		/// </summary>
		public string[] Properties { get; set; }

		public OrganizationIndexAttribute()
		{
		}

		public OrganizationIndexAttribute(params string[] properties)
		{
			this.Properties = properties;
		}
	}
}