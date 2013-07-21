using System.Collections.Generic;

namespace Shaolinq.Persistence.Sql
{
	public class TableIndexDescriptor
	{
		public string Name { get; set; }
		public List<ColumnDescriptor> Columns { get; set; }

		public TableIndexDescriptor()
		{
			this.Columns = new List<ColumnDescriptor>();
		}
	}
}
