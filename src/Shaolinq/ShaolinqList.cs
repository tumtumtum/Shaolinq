// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Platform.Collections;

namespace Shaolinq
{
	public class ShaolinqList
	{
		public static PropertyInfo GetChangedProperty(Type type)
		{
			return typeof(ShaolinqList<>).MakeGenericType(type).GetProperty("Changed");
		}
	}

	public class ShaolinqList<T>
		: ArrayList<T>
	{
		public bool Changed { get; internal set; }

		public ShaolinqList()
		{
			this.AfterCleared += HandleChanged;
			this.AfterItemAdded += HandleChanged;
			this.AfterItemChanged += HandleChanged;
			this.AfterItemRemoved += HandleChanged;
		}

		public ShaolinqList(IEnumerable<T> list)
		{
			this.AfterCleared += HandleChanged;
			this.AfterItemAdded += HandleChanged;
			this.AfterItemChanged += HandleChanged;
			this.AfterItemRemoved += HandleChanged;

			if (list != null)
			{
				foreach (var value in list)
				{
					this.Add(value);
				}
			}
		}

		private void HandleChanged(object sender, EventArgs eventArgs)
		{
			this.Changed = true;
		}
	}
}
