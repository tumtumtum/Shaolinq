using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence
{
	public interface IPersistenceQueryProvider
		: IQueryProvider
	{
		IRelatedDataAccessObjectContext RelatedDataAccessObjectContext 
		{
			get;
			set;
		}

		string GetQueryText(Expression expression);
	}
}
