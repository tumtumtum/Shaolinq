// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Persistence
{
	public class MigrationPropertyInfo
	{
		public long CurrentSize
		{
			get;
			set;
		}

		public long NewSize
		{
			get;
			set;
		}

		public string PropertyName
		{
			get;
			set;
		}

		public string PersistedName
		{
			get;
			set;
		}

		public Type OldType
		{
			get;
			set;
		}

		public PropertyDescriptor PropertyDescriptor
		{
			get;
			set;
		}
	}
}
