using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class AssemblyBuildContext
		: IDisposable
	{
		public Assembly SourceAssembly { get; set; }
		public Assembly TargetAssembly { get; set; }
		public Dictionary<Type, DataAccessObjectTypeBuilder> TypeBuilders { get; private set; }

		public AssemblyBuildContext()
		{
			this.TypeBuilders = new Dictionary<Type, DataAccessObjectTypeBuilder>();
		}

		public virtual void Dispose()
		{
		}
	}
}
