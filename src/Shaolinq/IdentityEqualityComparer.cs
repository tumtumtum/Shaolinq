// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shaolinq
{
	internal class IdentityEqualityComparer<T>
		: IEqualityComparer<T>
	{
		public static readonly IdentityEqualityComparer<T> Default = new IdentityEqualityComparer<T>();

		public bool Equals(T x, T y)
		{
			return Object.ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
