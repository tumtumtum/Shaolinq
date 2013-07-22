using System;
using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class FieldInfoFastRef
	{
		public static readonly FieldInfo GuidEmptyGuid = typeof(Guid).GetField("Empty", BindingFlags.Public | BindingFlags.Static);
		public static readonly FieldInfo PropertyInfoAndValueValueField = typeof(PropertyInfoAndValue).GetField("value", BindingFlags.Instance | BindingFlags.Public);
	}
}
