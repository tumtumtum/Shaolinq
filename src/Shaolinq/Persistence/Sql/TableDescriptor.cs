// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;

namespace Shaolinq.Persistence.Sql
{
	public class TableDescriptor
	{
		public List<ColumnDescriptor> Columns { get; set; }
		public List<TableIndexDescriptor> Indexes { get; set; }

		public TableDescriptor()
		{
			this.Columns = new List<ColumnDescriptor>();
			this.Indexes = new List<TableIndexDescriptor>();
		}
	}
}
