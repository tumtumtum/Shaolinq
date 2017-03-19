// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class IndexAttribute
		: Attribute, ICloneable
	{
		/// <summary>
		/// The index should be a unique index
		/// </summary>
		public bool Unique { get; set; }

		/// <summary>
		/// The data of this column will be included in the leaf nodes of the index but will not
		/// actually be indexed. Equivalent to <c>CREATE INDEX INCLUDE</c> on Microsfot SQL Server.
		/// </summary>
		public bool DontIndexButIncludeValue { get; set; }

		/// <summary>
		/// The value of strings will be lower cased before creating the index
		/// </summary>
		public bool LowercaseIndex { get; set; }

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		public int CompositeOrder { get; set; }

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
		/// Whether the current property will be ordered in ascending or descending order in the index
		/// </summary>
		/// <remarks>
		/// <seealso cref="SortOrder"/>
		/// </remarks>
		public SortOrder SortOrder { get; set; }

		public IndexAttribute()
			: this(null, false)
		{
		}

		public IndexAttribute(string indexName)
			: this(indexName, false)
		{
		}

		public IndexAttribute(string indexName, bool unique)
		{
			this.IndexName = indexName;
			this.Unique = unique;
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}
