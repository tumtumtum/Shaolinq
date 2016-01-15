// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectionBuilderScope
	{
		public Dictionary<string, int> ColumnIndexes { get; }

		public ProjectionBuilderScope(string[] columnNames)
			: this((Dictionary<string, int>)columnNames.Select((c, i) => new { c, i }).ToDictionary(c => c.c, c => c.i))
		{	
		}

		public ProjectionBuilderScope(Dictionary<string, int> columnIndexes)
		{
			this.ColumnIndexes = columnIndexes;
		}
	}
}