// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence.Sql
{
	public enum SqlFeature
	{
		None,
		Constraints,
		SelectForUpdate,
		AlterTableAddConstraints,
		IndexNameCasing,
		IndexToLower
	}
}
