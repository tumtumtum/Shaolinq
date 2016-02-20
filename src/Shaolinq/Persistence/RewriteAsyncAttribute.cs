// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	namespace Persistence
	{
		[AttributeUsage(AttributeTargets.Method)]
		public class RewriteAsyncAttribute
			: Attribute
		{
			public RewriteAsyncAttribute(bool promoteToPublic = false)
			{
			}
		}
	}
}
