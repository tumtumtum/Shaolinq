// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence.Linq
{
	internal struct LeftRightJoinInfo<L, R>
	{
		public L Left { get; set; }
		public R Right { get; set; }
	}
}