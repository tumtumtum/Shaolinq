using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class IndexAttribute
		: Attribute
	{
		public bool Unique { get; set; }
		public bool LowercaseIndex { get; set; }
		public int CompositeOrder { get; set; }
		public string IndexName { get; set; }
		public IndexType IndexType { get; set; }

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
	}
}
