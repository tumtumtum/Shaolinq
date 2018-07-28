// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Platform;

namespace Shaolinq.TypeBuilding
{
	public class FieldInfoFastRef
	{
		public static readonly FieldInfo GuidEmptyGuid = TypeUtils.GetField(() => Guid.Empty);
	}
}
