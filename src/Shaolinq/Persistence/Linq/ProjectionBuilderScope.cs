// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectionBuilderScope
	{
		public Dictionary<string, int> ColumnIndexes { get; }
		public readonly List<Expression> rootPrimaryKeys = new List<Expression>();

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