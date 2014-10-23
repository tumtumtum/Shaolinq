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
		public bool DisableConstraint { get; set; }
		public ForeignObjectAction OnDeleteAction { get; set; }
		public ForeignObjectAction OnUpdateAction { get; set; }

		public ForeignObjectConstraintAttribute(bool disableConstraint = false)
		{
			this.DisableConstraint = true;
		}
	}
}
