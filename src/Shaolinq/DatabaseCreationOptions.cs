using System;

namespace Shaolinq
{
	[Flags]
	public enum DatabaseCreationOptions
	{
		IfNotExist,
		DeleteExisting
	}
}
