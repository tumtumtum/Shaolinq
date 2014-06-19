// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectedColumns
	{
		public Expression Projector { get; private set; }
		public ReadOnlyCollection<SqlColumnDeclaration> Columns { get; private set; }

		public ProjectedColumns(Expression projector, ReadOnlyCollection<SqlColumnDeclaration> columns)
		{
			this.Columns = columns;
			this.Projector = projector;
		}
	}
}
