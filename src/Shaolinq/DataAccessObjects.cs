// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public class DataAccessObjects<T>
		: DataAccessObjectsQueryable<T>
		where T : class, IDataAccessObject
	{
		public DataAccessObjects()
		{	
		}

		public DataAccessObjects(DataAccessModel dataAccessModel, Expression expression)
		{
			this.Initialize(dataAccessModel, expression);
		}
	}
}
