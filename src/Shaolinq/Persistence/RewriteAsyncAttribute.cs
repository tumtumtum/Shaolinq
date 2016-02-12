// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	namespace Persistence
	{
		[AttributeUsage(AttributeTargets.Method)]
		public class RewriteAsyncAttribute
			: Attribute
		{
			public RewriteAsyncAttribute(bool withOverride = false)
			{
			}
		}
	}
}
