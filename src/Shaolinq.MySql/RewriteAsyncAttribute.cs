// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace Shaolinq.MySql
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
	internal class RewriteAsyncAttribute
		: Attribute
	{
		public bool ContinueOnCapturedContext { get; }
		public MethodAttributes MethodAttributes { get; }

		public RewriteAsyncAttribute(MethodAttributes methodAttributes = default(MethodAttributes), bool continueOnCapturedContext = false)
		{
			this.MethodAttributes = methodAttributes;
			this.ContinueOnCapturedContext = continueOnCapturedContext;
		}
	}
}
