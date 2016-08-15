// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence
{
	public enum SqlCapability
	{
		None,
		Constraints,
		Deferrability,
		SelectForUpdate,
		AlterTableAddConstraints,
		IndexNameCasing,
		IndexToLower,
		InsertOutput,
		IndexInclude,
		InsertIntoReturning,
		ForeignKeys,
		InlineForeignKeys,
		ConstrainRestrictAction,
		RestrictAction,
		DeleteAction,
		CascadeAction,
		SetNullAction,
		SetDefaultAction,
		UpdateAutoIncrementColumns,
		SetIdentityInsert,
		MultipleActiveResultSets,
		CrossApply,
		OuterApply
	}
}
