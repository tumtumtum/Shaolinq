// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.AsyncRewriter.Tests
{
	[RewriteAsync]
	public partial interface ICommand
	{
		void Test();
	}
}
