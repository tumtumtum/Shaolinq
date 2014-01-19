using System;

namespace Shaolinq.Persistence.Linq
{
	[Flags]
	public enum SqlDataDefinitionBuilderFlags
	{
		None,
		BuildTables = 1,
		BuildIndexes = 1 << 1,
		BuildEnums = 1 << 2
	}
}
