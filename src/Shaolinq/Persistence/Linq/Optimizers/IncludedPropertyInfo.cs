using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class IncludedPropertyInfo
	{
		public Expression RootExpression { get; set; }
		public PropertyPath PropertyPath { get; set; }
		
		public override string ToString()
		{
			return string.Format("Path:{0}, Root:{1}", PropertyPath, RootExpression);
		}
	}
}