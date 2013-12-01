// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Collections.Generic;
using Shaolinq.Persistence.Sql;

namespace Shaolinq.Persistence
{
	public class MigrationTypeInfo
	{
		public string TypeName { get; set; }
		public TypeDescriptor TypeDescriptor { get; private set; }
		public List<MigrationPropertyInfo> NewProperties { get; private set; }
		public List<MigrationPropertyInfo> ModifiedProperties { get; private set; }
		public List<MigrationPropertyInfo> OldProperties { get; private set; }
		public List<IndexDescriptor> NewIndexes { get; set; }
		public List<TableIndexDescriptor> OldIndexes { get; set; }

		public MigrationTypeInfo(DataAccessModel model, TypeDescriptor typeDescriptor)
			: this()
		{
			this.TypeDescriptor = typeDescriptor;
			this.TypeName = typeDescriptor.GetPersistedName(model);
		}

		public MigrationTypeInfo()
		{
			this.OldProperties = new List<MigrationPropertyInfo>();
			this.NewProperties = new List<MigrationPropertyInfo>();
			this.ModifiedProperties = new List<MigrationPropertyInfo>();
			this.NewIndexes = new List<IndexDescriptor>();
			this.OldIndexes = new List<TableIndexDescriptor>();
		}
	}
}
