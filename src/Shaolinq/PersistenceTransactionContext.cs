// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using Shaolinq.Persistence.Sql;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq
{
	public abstract class PersistenceTransactionContext
		: IDisposable
	{
		public abstract bool IsClosed { get; }

		public abstract void Delete(SqlDeleteExpression deleteExpression);
		public abstract void Delete(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		public abstract int Update(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		public abstract InsertResults Insert(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		
		public virtual void Commit()
		{
		}

		public virtual void Rollback()
		{
		}

		public virtual void Dispose()
		{
		}
	}
}
