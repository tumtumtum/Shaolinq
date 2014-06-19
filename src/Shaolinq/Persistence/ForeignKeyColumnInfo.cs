// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence
{
	public struct ForeignKeyColumnInfo
	{
		public string ColumnName { get; set; }
		public TypeDescriptor ForeignType { get; set; }
		public PropertyDescriptor KeyPropertyOnForeignType { get; set; }
		public PropertyDescriptor OjbectPropertyOnReferencingType { get; set; }
	}
}
