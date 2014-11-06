using System.Collections.Generic;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public class PropertyPathEqualityComparer
		: IEqualityComparer<PropertyPath>
	{
		public static readonly PropertyPathEqualityComparer Default = new PropertyPathEqualityComparer();

		public bool Equals(PropertyPath x, PropertyPath y)
		{
			return ArrayEqualityComparer<PropertyInfo>.Default.Equals(x.path, y.path);
		}

		public int GetHashCode(PropertyPath obj)
		{
			return obj.GetHashCode();
		}
	}
}