// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class ConstructorInfoFastRef
	{
		public static readonly ConstructorInfo InvalidOperationExpceptionConstructor = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });
		public static readonly ConstructorInfo WriteOnlyDataAccessObjectExceptionConstructor = typeof(WriteOnlyDataAccessObjectException).GetConstructor(new Type[] { typeof(IDataAccessObjectAdvanced) });
		public static readonly ConstructorInfo ObjectPropertyValueConstructor = typeof(ObjectPropertyValue).GetConstructor(new[] { typeof(Type), typeof(string), typeof(string), typeof(int), typeof(object) });
		public static readonly ConstructorInfo ObjectPropertyValueListConstructor = typeof(List<ObjectPropertyValue>).GetConstructor(new[] { typeof(int) });		
	}
}
