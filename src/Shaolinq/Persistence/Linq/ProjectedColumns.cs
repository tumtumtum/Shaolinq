// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectedColumns
	{
		public Expression Projector { get; }
		public IReadOnlyList<SqlColumnDeclaration> Columns { get; }

		public ProjectedColumns(Expression projector, IReadOnlyList<SqlColumnDeclaration> columns)
		{
			this.Columns = columns;
			this.Projector = projector;
		}
	}
}
