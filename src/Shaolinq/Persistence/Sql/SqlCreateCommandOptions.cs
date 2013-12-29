using System;

namespace Shaolinq.Persistence.Sql
{
	[Flags]
	public enum SqlCreateCommandOptions
	{
		None = 0,
		UnpreparedExecute = 1,
		Default = None
	}
}
