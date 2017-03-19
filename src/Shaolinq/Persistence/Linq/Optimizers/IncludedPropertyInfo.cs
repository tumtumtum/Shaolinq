// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class IncludedPropertyInfo
	{
		public Expression RootExpression { get; set; }
		public PropertyPath FullAccessPropertyPath { get; set; }
		public PropertyPath IncludedPropertyPath { get; set; }
		
		public override string ToString()
		{
			return $"Path:{this.FullAccessPropertyPath}, Root:{this.RootExpression}";
		}
	}
}