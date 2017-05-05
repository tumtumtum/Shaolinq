using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class OrganizationIndexAttribute
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
		/// Set to true on a primary key to remove the default organization index and use heap storage instead (MSSQL only)
		/// </summary>
		public bool Disable { get; set; } 

		/// <summary>
		/// The data of this column will be included in the leaf nodes of the index but will not
		/// actually be indexed. Equivalent to <c>CREATE INDEX INCLUDE</c> in MSSQL
		/// </summary>
		public bool IncludeOnly { get; set; }
		
		/// <summary>
		/// The name of the index. Some databases require index names to be server-unique rather than
		/// database-unique. Use the same <c>IndexName</c> across multiple properties to create
		/// a composite index
		/// </summary>
		public string IndexName { get; set; }

		/// <summary>
		/// If supported, the value of strings will be lower cased before creating the index
		/// </summary>
		public bool LowercaseIndex { get; set; }
	}
}