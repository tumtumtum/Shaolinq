using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq
{
	public class UniqueKeyConstraintException
		: DataAccessException
	{
		public UniqueKeyConstraintException(Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{	
		}
	}
}
