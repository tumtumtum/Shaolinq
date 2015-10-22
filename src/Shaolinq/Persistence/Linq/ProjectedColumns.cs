// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Platform.Collections;
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
