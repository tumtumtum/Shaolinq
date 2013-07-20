using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	internal class ReflectionEmittedAttribute
		: Attribute
	{
	}
}
