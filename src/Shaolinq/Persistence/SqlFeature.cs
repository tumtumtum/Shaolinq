// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence
{
	public enum SqlFeature
	{
		None,
		Constraints,
		Deferrability,
		SelectForUpdate,
		AlterTableAddConstraints,
		IndexNameCasing,
		IndexToLower,
		InsertIntoReturning,
		SupportsAndPrefersInlineForeignKeysWherePossible
	}
}
