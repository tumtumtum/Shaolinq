using System;

namespace Shaolinq
{
	public enum ForeignObjectAction
	{
		Default,
		NoAction,
		Restrict,
		Cascade,
		SetNull,
		SetDefault
	}

	public class ForeignObjectConstraintAttribute
		: Attribute
	{
		public ForeignObjectAction OnDeleteAction { get; set; }
		public ForeignObjectAction OnUpdateAction { get; set; }

		public ForeignObjectConstraintAttribute()
		{
		}
	}
}
