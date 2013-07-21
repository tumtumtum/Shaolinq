using System.Reflection;

namespace Shaolinq.TypeBuilding
{
	public class FieldInfoFastRef
	{
		public static readonly FieldInfo PropertyInfoAndValueValueField = typeof(PropertyInfoAndValue).GetField("value", BindingFlags.Instance | BindingFlags.Public);
	}
}
