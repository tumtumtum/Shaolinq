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
			return string.Format("Path:{0}, Root:{1}", this.FullAccessPropertyPath, RootExpression);
		}
	}
}