using System;
using System.Collections.Generic;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ComputedMemberAttribute
		: Attribute
	{
		public string Expression { get; set; }

		public ComputedMemberAttribute(string expression)
		{
		}

		public IEnumerable<string> GetPropertyReferences()
		{
			yield break;
		}
	}
}