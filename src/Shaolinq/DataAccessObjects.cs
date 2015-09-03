// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq
{
	public class DataAccessObjects<T>
		: DataAccessObjectsQueryable<T>
		where T : DataAccessObject
	{
		public DataAccessObjects(DataAccessModel dataAccessModel, Expression expression)
			: base(dataAccessModel, expression)
		{
		}

		public virtual T GetReference<K>(K primaryKey)
		{
			return GetReference(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetReference<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return this.DataAccessModel.GetReference<T, K>(primaryKey, primaryKeyType);
		}

		public virtual T GetReference<K>(Expression<Func<T, K>> condition)
		{
			return null;
			//return this.DataAccessModel.GetReference<T, K>(condition);
		}
	}
}
