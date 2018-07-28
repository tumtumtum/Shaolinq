// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Apply this attribute to one or more properties to define indexes for those properties.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Applying this property to multiple properties will create a composite index. You can
	/// define the order of the columns of a compiosite index by setting the <see cref="IndexAttribute.CompositeOrder"/>
	/// property. By default properties with no explicitly defined <see cref="IndexAttribute.CompositeOrder"/> come last
	/// in source code order.
	/// </para>
	/// <para>
	/// It is easier to define composite indexes by applying this attribute to the <see cref="DataAccessObject"/> class
	/// and using the <see cref="IndexAttribute(string[])"/> constructor to specify the properties for the index. The order
	/// of the properties provided defines the order within the index.
	/// </para>
	/// <para>
	/// If defined on a class the <see cref="Properties"/> property will contain a list of string property specifiers.
	/// Each string is of the format <c>PropertyName:[Modifier1,Modifier2]</c> where valid modifiers can be:
	/// <c>Ascending</c>, <c>Descending</c>, <c>LowercaseIndex</c>, <c>IncludeOnly</c>.
	/// </para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
	public class IndexAttribute
		: Attribute
	{
		internal readonly string indexNameIfPropertyOrPropertyIfClass;

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
		/// The index should be a unique index
		/// </summary>
		public bool Unique { get; set; }

		/// <summary>
		/// The data of this column will be included in the leaf nodes of the index but will not
		/// actually be indexed. Equivalent to <c>CREATE INDEX INCLUDE</c> in MSSQL
		/// </summary>
		/// <remarks>
		/// This property is ignored if the attribute is applied at the class level.
		/// </remarks>
		public bool IncludeOnly { get; set; }

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
		/// Multiple conditions can be added bvy using the <c>&amp;&amp;</c> operator. If <see cref="Condition"/> is defined on 
		/// more than one related <see cref="IndexAttribute"/> then they are all included.
		/// </remarks>
		/// <example>
		/// [IndexAttribute(Condition = "Master == true")]
		/// </example>
		public string Condition { get; set; }

		/// <summary>
		/// The properties that make up the index (only applicable if attribute is used on a class)
		/// </summary>
		public string[] Properties { get; set; }

		/// <summary>
		/// Creates a single column index for this property.
		/// </summary>
		public IndexAttribute()
		{
		}

		/// <summary>
		/// Create a new index with the given name if this atteribute is used on a property or an index for the given property if used on a class
		/// </summary>
		/// <remarks>
		/// <para>If this attribute is used on a property then using the same index name across different properties creates a composite index.</para>
		/// <para>
		/// If this attribute is used on a class then the supplied paramter defines the single property that this index applies to.
		/// In this case use the <see cref="IndexName"/> property to optionally set the IndexName name.
		/// </para>
		/// </remarks>
		/// <param name="indexNameIfPropertyOrPropertyIfClass">The name of the index (if used on a property) or name of the property (if used on a class)</param>
		public IndexAttribute(string indexNameIfPropertyOrPropertyIfClass)
		{
			this.indexNameIfPropertyOrPropertyIfClass = indexNameIfPropertyOrPropertyIfClass;
		}

		/// <summary>
		/// Create a new index with the given properties
		/// </summary>
		/// <param name="properties">The properties </param>
		public IndexAttribute(params string[] properties)
		{
			this.Properties = properties;
		}
	}
}
