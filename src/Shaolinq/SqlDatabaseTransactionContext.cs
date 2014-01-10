// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
﻿using System.Data;
﻿using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	public abstract class SqlDatabaseTransactionContext
		: IDisposable
	{
		public abstract bool IsClosed { get; }
		public IDbConnection DbConnection { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }

		public abstract void Delete(SqlDeleteExpression deleteExpression);
		public abstract void Delete(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		public abstract int Update(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		public abstract InsertResults Insert(Type type, IEnumerable<IDataAccessObject> dataAccessObjects);
		
		protected SqlDatabaseTransactionContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection dbConnection)
		{	
			this.DbConnection = dbConnection;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}

		public virtual IDbCommand CreateCommand()
		{
			return CreateCommand(SqlCreateCommandOptions.Default);
		}

		public virtual IDbCommand CreateCommand(SqlCreateCommandOptions options)
		{
			var retval = this.DbConnection.CreateCommand();

			retval.CommandTimeout = (int)this.SqlDatabaseContext.CommandTimeout.TotalSeconds;

			return retval;
		}

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
