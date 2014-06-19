// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class ConstructorInfoFastRef
	{
		public static readonly ConstructorInfo InvalidOperationExpceptionConstructor = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });
		public static readonly ConstructorInfo WriteOnlyDataAccessObjectExceptionConstructor = typeof(WriteOnlyDataAccessObjectException).GetConstructor(new Type[] { typeof(IDataAccessObject) });
	}
}
