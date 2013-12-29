// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	public class DatabaseTransactionContextWrapper
		: DatabaseTransactionContext
	{
		protected readonly DatabaseTransactionContext wrappee;

		public DatabaseTransactionContextWrapper(DatabaseTransactionContext wrappee)
		{
			this.wrappee = wrappee;
		}

		public override bool IsClosed
		{
			get
			{
				return this.wrappee.IsClosed;
			}
		}

		public override void Delete(SqlDeleteExpression deleteExpression)
		{
			this.wrappee.Delete(deleteExpression);
		}

		public override void Delete(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
		{
			this.wrappee.Delete(type, dataAccessObjects);
		}

		public override int Update(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
		{
			return this.wrappee.Update(type, dataAccessObjects);
		}

		public override InsertResults Insert(Type type, IEnumerable<IDataAccessObject> dataAccessObjects)
		{
			return this.wrappee.Insert(type, dataAccessObjects);
		}

		public override void Commit()
		{
			this.wrappee.Commit();
		}

		public override void Rollback()
		{
			this.wrappee.Rollback();
		}

		public override void Dispose()
		{
			this.wrappee.Dispose();
		}
	}
}
