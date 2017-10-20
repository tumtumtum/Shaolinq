// Copyright (c) 2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace Shaolinq.AsyncRewriter.Tests
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
	internal class RewriteAsyncAttribute
		: Attribute
	{
		public bool ContinueOnCapturedContext { get; set; }
		public MethodAttributes MethodAttributes { get; set; }
		public bool ApplyToDescendents { get; set; }

		public RewriteAsyncAttribute()
			: this(default(MethodAttributes))
		{
		}

		public RewriteAsyncAttribute(MethodAttributes methodAttributes)
		{
			this.MethodAttributes = methodAttributes;
		}
	}
}
