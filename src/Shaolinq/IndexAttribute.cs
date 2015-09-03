// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class IndexAttribute
		: Attribute, ICloneable
	{
		public bool Unique { get; set; }
		public bool LowercaseIndex { get; set; }
		public int CompositeOrder { get; set; }
		public string IndexName { get; set; }
		public IndexType IndexType { get; set; }
		public SortOrder SortOrder { get; set; }

		public IndexAttribute()
			: this(null, false)
		{
		}

		public IndexAttribute(string indexName)
			: this(indexName, false)
		{
		}

		public IndexAttribute(string indexName, bool unique)
		{
			this.IndexName = indexName;
			this.Unique = unique;
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}
