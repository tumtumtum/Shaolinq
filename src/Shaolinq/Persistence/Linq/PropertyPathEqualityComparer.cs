// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Reflection;
using Platform;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

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