// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	internal struct GroupByInfo
	{
		public string Alias { get; }
		public Expression Element { get; }

		public GroupByInfo(string alias, Expression element)
			: this()
		{
			this.Alias = alias;
			this.Element = element;
		}
	}
}
