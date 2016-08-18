// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shaolinq.TypeBuilding
{
	public class AssemblyBuildContext
	{
		public Assembly TargetAssembly { get; }
		public TypeBuilder DataAccessModelPropertiesTypeBuilder { get; }
		public TypeBuilder DataAccessModelTypeBuilder { get; set; }
		public FieldInfo DataAccessModelPropertiesTypeBuilderField { get; set; }
		public Dictionary<Type, DataAccessObjectTypeBuilder> TypeBuilders { get; }

		public AssemblyBuildContext(Assembly targetAssembly, TypeBuilder dataAccessModelPropertiesTypeBuilder)
		{
			this.TargetAssembly = targetAssembly;
			this.DataAccessModelPropertiesTypeBuilder = dataAccessModelPropertiesTypeBuilder;
			this.TypeBuilders = new Dictionary<Type, DataAccessObjectTypeBuilder>();
		}
	}
}
