using System;

namespace Shaolinq
{
	[Flags]
	public enum IndexType
	{
		Unique,
		Hash,
		BTree,
		RTree
	}
}
