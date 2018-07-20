using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence
{
	public class IndexInfo
	{
		public bool Unique { get; }
		public string IndexName {get;}
		public string Condition { get; }
		public IndexType IndexType { get; }
		public IReadOnlyList<IndexPropertyInfo> Properties { get; set; }

		public IndexInfo(string indexName, IReadOnlyList<IndexPropertyInfo> properties, bool unique, IndexType indexType, string condition)
		{
			this.IndexType = indexType;
			this.Condition = condition;
			this.Properties = properties;
			this.Unique = unique;
			this.IndexName = indexName;
		}
	}

	public class IndexPropertyInfo
	{
		public bool Lowercase { get; }
		public string PropertyName { get; }
		public SortOrder SortOrder { get; }
		public bool IncludeOnly { get; }
		public string Condition {get;}

		public IndexPropertyInfo(string property)
		{
			var ss = property.Split(':');

			this.PropertyName = ss[0];

			if (ss.Length == 1)
			{
				return;
			}

			foreach (var value in ss[1].Split(',').Select(c => c.Trim().ToLower()))
			{
				switch (value)
				{
				case "lowercase":
					this.Lowercase = true;
					break;
				case "ascending":
					this.SortOrder = SortOrder.Ascending;
					break;
				case "descending":
					this.SortOrder = SortOrder.Descending;
					break;
				case "includeonly":
					this.IncludeOnly = true;
					break;
				}
			}
		}

		public IndexPropertyInfo(string propertyName, bool lowercase, SortOrder sortOrder, bool includeOnly, string condition)
		{
			this.PropertyName = propertyName;
			this.Lowercase = lowercase;
			this.SortOrder = sortOrder;
			this.IncludeOnly = includeOnly;
			this.Condition = condition;
		}
	}
}
