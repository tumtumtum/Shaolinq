using System;

namespace Shaolinq.TypeBuilding
{
	public abstract class DataAccessAssemblyProvider
	{
		public abstract RuntimeDataAccessModelInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration);
	}
}