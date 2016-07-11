// Copyright (c) 2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace $rootnamespace$
{
	[AttributeUsage(AttributeTargets.Method)]
	internal class RewriteAsyncAttribute
		: Attribute
	{
		public MethodAttributes MethodAttributes { get; private set; }

		public RewriteAsyncAttribute(MethodAttributes methodAttributes = default(MethodAttributes))
		{
			this.MethodAttributes = methodAttributes;
		}
	}
}
