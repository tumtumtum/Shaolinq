// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;

namespace Shaolinq.Persistence
{
	public struct ColumnInfo
	{
		public string ColumnName { get; set; }
		public TypeDescriptor ForeignType { get; set; }
		public PropertyDescriptor[] VisitedProperties { get; set; }
		public PropertyDescriptor DefinitionProperty { get; set; }

		private string fullParentName;
		private string fullPropertyName;

		public string GetFullParentName()
		{
			if (fullParentName == null)
			{
				fullParentName = string.Concat(this.VisitedProperties.Select(c => c.PropertyName));
			}

			return fullParentName;
		}

		public string GetFullPropertyName()
		{
			if (fullPropertyName == null)
			{
				fullPropertyName = this.GetFullParentName() + this.DefinitionProperty.PropertyName;
			}

			return fullPropertyName;
		}
	}
}
