using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessObjectAttribute
		: Attribute
	{
		public string Name { get; set; }
		public bool Abstract { get; set; }

		public string GetName(Type type)
		{
			return this.Name ?? type.Name;
		}
	}
}
