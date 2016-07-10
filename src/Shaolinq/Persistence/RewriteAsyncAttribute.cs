// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace Shaolinq.Persistence
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RewriteAsyncAttribute
		: Attribute
	{
		public RewriteAsyncAttribute(MethodAttributes methodAttributes = new MethodAttributes())
		{
		}
	}
}
