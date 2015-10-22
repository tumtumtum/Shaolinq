// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
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
		InsertOutput,
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
		PragmaIdentityInsert
	}
}
