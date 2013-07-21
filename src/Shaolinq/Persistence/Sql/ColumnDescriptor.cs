using System;

namespace Shaolinq.Persistence.Sql
{
	public class ColumnDescriptor
	{
		public Type DataType { get; set; }
		public int DataLength { get; set; }
		public string ColumnName { get; set; }
		
		public override string ToString()
		{
			return this.ColumnName;
		}
	}
}
