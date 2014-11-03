using System;

namespace Shaolinq.TypeBuilding
{
	public abstract class DataAccessAssemblyProvider
	{
		public abstract AssemblyBuildInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration);
	}
}