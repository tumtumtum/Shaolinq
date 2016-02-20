// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq
{
	public enum DataAccessIsolationLevel
	{
		Serializable,
		RepeatableRead,
		ReadCommitted,
		ReadUncommitted,
		Snapshot,
		Chaos,
		Unspecified
	}
}
