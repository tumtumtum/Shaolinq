using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class IncludedPropertyInfo
	{
		public Expression RootExpression { get; set; }
		public PropertyPath PropertyPath { get; set; }
		public PropertyPath SuffixPropertyPath { get; set; }
	}
}