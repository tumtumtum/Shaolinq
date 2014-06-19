// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence
{
	public enum SqlFeature
	{
		None,
		Constraints,
		SelectForUpdate,
		AlterTableAddConstraints,
		IndexNameCasing,
		IndexToLower,
		InsertIntoReturning,
		SupportsAndPrefersInlineForeignKeysWherePossible
	}
}
