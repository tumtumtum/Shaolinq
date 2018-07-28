// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq
{
	public interface IIndexAttribute
	{
		/// <summary>
		/// The order of the index. Unspecified is database dependent but usually ascending.
		/// </summary>
		SortOrder SortOrder { get; set; }

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		int CompositeOrder { get; set; }
		
		/// <summary>
		/// The data of this column will be included in the leaf nodes of the index but will not
		/// actually be indexed. Equivalent to <c>CREATE INDEX INCLUDE</c> in MSSQL
		/// </summary>
		bool IncludeOnly { get; set; }
	}
}