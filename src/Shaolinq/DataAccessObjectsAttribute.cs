using System;

namespace Shaolinq
{
	/// <summary>
	/// Marks a propertyas one that references a collection of objects.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class DataAccessObjectsAttribute
		: Attribute
	{
	}
}
