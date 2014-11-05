using System.Collections.Generic;
using System.Linq;
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
			return ArrayEqualityComparer<PropertyInfo>.Default.Equals(x.Path, y.Path);
		}

		public int GetHashCode(PropertyPath obj)
		{
			return obj.Path.Aggregate(0, (current, path) => current ^ path.Name.GetHashCode());
		}
	}
}