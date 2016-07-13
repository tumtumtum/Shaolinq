// Copyright (c) 2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace $rootnamespace$
{
	[AttributeUsage(AttributeTargets.Method)]
	internal class RewriteAsyncAttribute
		: Attribute
	{
    public bool ContinueOnCapturedContext { get; private set; }
		public MethodAttributes MethodAttributes { get; private set; }

		public RewriteAsyncAttribute(bool continueOnCapturedContext = false, MethodAttributes methodAttributes = default(MethodAttributes))
		{
			this.MethodAttributes = methodAttributes;
			this.ContinueOnCapturedContext = continueOnCapturedContext;
		}
	}
}
