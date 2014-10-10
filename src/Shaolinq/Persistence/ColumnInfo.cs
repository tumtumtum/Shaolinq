// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;

namespace Shaolinq.Persistence
{
	public struct ColumnInfo
	{
		public TypeDescriptor ForeignType { get; set; }
		public PropertyDescriptor[] VisitedProperties { get; set; }
		public PropertyDescriptor DefinitionProperty { get; set; }
		public string ColumnName { get { return this.GetColumnName(); } }

		private string columnName;
		private string tailColumnName;
		private string fullParentName;
		private string fullPropertyName;

		public string GetTailColumnName()
		{
			if (this.VisitedProperties.Length == 0)
			{
				throw new InvalidOperationException();
			}

			if (tailColumnName == null)
			{
				tailColumnName = string.Join("", this.VisitedProperties.Skip(1).Select(c => c.PersistedName));

				if (this.VisitedProperties.Length == 1)
				{
					tailColumnName += this.DefinitionProperty.PersistedName;
				}
				else
				{
					tailColumnName += this.DefinitionProperty.PersistedShortName;
				}
			}

			return tailColumnName;
		}

		public string GetColumnName()
		{
			if (columnName == null)
			{
				columnName = string.Join("", this.VisitedProperties.Select(c => c.PersistedName));

				if (this.VisitedProperties.Length == 0)
				{
					columnName += this.DefinitionProperty.PersistedName;
				}
				else
				{
					columnName += this.DefinitionProperty.PersistedShortName;
				}
			}

			return columnName;
		}

		public string GetFullParentName()
		{
			if (fullParentName == null)
			{
				fullParentName = string.Join(".", this.VisitedProperties.Select(c => c.PropertyName));
			}

			return fullParentName;
		}

		public string GetFullPropertyName()
		{
			if (fullPropertyName == null)
			{
				fullPropertyName = string.Join(".", this.VisitedProperties.Select(c => c.PropertyName).Concat(new [] { this.DefinitionProperty.PropertyName }));
			}

			return fullPropertyName;
		}

		public override string ToString()
		{
			return this.ColumnName;
		}
	}
}
