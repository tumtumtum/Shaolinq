// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class AssemblyBuildContext
	{
		public Assembly TargetAssembly { get; }
		public Dictionary<Type, DataAccessObjectTypeBuilder> TypeBuilders { get; }

		public AssemblyBuildContext(Assembly targetAssembly)
		{
			this.TargetAssembly = targetAssembly;
			this.TypeBuilders = new Dictionary<Type, DataAccessObjectTypeBuilder>();
		}
	}
}
