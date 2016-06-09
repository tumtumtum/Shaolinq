using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject(NotPersisted = true)]
	public abstract	class BaseGenericDao<T> : DataAccessObject<long>
	{
		[BackReference]
		public abstract T RelatedObject { get; set; }
	}

	[DataAccessObject]
	public abstract class ConcreteGenericDao : BaseGenericDao<School>
	{
	}
}
