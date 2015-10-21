// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class ForeignObjectConstraintAttribute
		: Attribute
	{
		public ForeignObjectAction OnDeleteAction { get; set; }
		public ForeignObjectAction OnUpdateAction { get; set; }
	}
}
