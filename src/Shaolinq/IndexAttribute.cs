// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Apply this property to one or more properties to define indexes for the properties.
	/// </summary>
	/// <remarks>
	/// Applying this property to multiple properties will create a composite index. You can
	/// define the order of the columns of a compiosite index by setting the <see cref="IndexAttribute.CompositeOrder"/>
	/// property. By default properties with no explicitly defined <see cref="IndexAttribute.CompositeOrder"/> come last
	/// in source code order.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class IndexAttribute
		: Attribute, IIndexAttribute
	{
		/// <summary>
		/// The order of the index. Unspecified is database dependent but usually ascending.
		/// </summary>
		public SortOrder SortOrder { get; set; }

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		public int CompositeOrder { get; set; } = int.MinValue;

		/// <summary>
		/// The index should be a unique index
		/// </summary>
		public bool Unique { get; set; }

		/// <summary>
		/// The data of this column will be included in the leaf nodes of the index but will not
		/// actually be indexed. Equivalent to <c>CREATE INDEX INCLUDE</c> in MSSQL
		/// </summary>
		public bool IncludeOnly { get; set; }

		/// <summary>
		/// If supported, the value of strings will be lower cased before creating the index
		/// </summary>
		public bool LowercaseIndex { get; set; }
			
		/// <summary>
		/// The name of the index. Some databases require index names to be server-unique rather than
		/// database-unique. Use the same <c>IndexName</c> across multiple properties to create
		/// a composite index
		/// </summary>
		public string IndexName { get; set; }

		/// <summary>
		/// The type of index to create if supported.
		/// </summary>
		/// <remarks>
		/// <seealso cref="IndexType"/>
		/// </remarks>
		public IndexType IndexType { get; set; }

		/// <summary>
		/// A string containing a conditional expression for determining what rows to include in the index
		/// </summary>
		/// <remarks>
		/// This property is useful for <see cref="Unique"/> composite indexes where you may only want to include rows
		/// in the index when certain columns are of a certain value.
		/// Multiple conditions can be added bvy using the <c>&&</c> operator. If <see cref="Condition"/> is defined on 
		/// more than one related <see cref="IndexAttribute"/> then they are all included.
		/// </remarks>
		/// <example>
		/// [IndexAttribute(Condition = "Master == true")]
		/// </example>
		public string Condition { get; set; }

		/// <summary>
		/// Creates a single column index for this property.
		/// </summary>
		public IndexAttribute()
		{
		}

		/// <summary>
		/// Create a new index with the given name. Using the same name across different properties creates a composite index.
		/// </summary>
		/// <param name="indexName">The name of the index</param>
		public IndexAttribute(string indexName)
		{
			this.IndexName = indexName;
		}
	}
}
