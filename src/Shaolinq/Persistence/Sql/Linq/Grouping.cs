// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class Grouping<K, V>
		: IGrouping<K, V>
	{
		public K Key { get; private set; }
		protected IEnumerable<V> Group { get; private set; }

		public Grouping(K key, IEnumerable<V> group)
		{
			this.Key = key;
			this.Group = group;
		}

		public IEnumerator<V> GetEnumerator()
		{
			return this.Group.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Group.GetEnumerator();
		}
	}
}
