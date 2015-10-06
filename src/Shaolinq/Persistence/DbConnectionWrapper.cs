// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;

namespace Shaolinq.Persistence
{
	public class DbConnectionWrapper
		: IDbConnection
	{
		public IDbConnection Inner { get; }

		public DbConnectionWrapper(IDbConnection inner)
		{
			this.Inner = inner;
		}

		public virtual void Dispose()
		{
			this.Inner.Dispose();
		}

		public virtual IDbTransaction BeginTransaction()
		{
			return this.Inner.BeginTransaction();
		}

		public virtual IDbTransaction BeginTransaction(IsolationLevel il)
		{
			return this.Inner.BeginTransaction(il);
		}

		public virtual void Close()
		{
			this.Inner.Close();
		}

		public virtual void ChangeDatabase(string databaseName)
		{
			this.Inner.ChangeDatabase(databaseName);
		}

		public virtual IDbCommand CreateCommand()
		{
			return this.Inner.CreateCommand();
		}

		public virtual void Open()
		{
			this.Inner.Open();
		}

		public virtual string ConnectionString
		{
			get
			{
				return this.Inner.ConnectionString;
			}
			set
			{
				this.Inner.ConnectionString = value;
			}
		}

		public virtual int ConnectionTimeout
		{
			get
			{
				return this.Inner.ConnectionTimeout;
			}
		}

		public virtual string Database
		{
			get
			{
				return this.Inner.Database;
			}
		}

		public virtual ConnectionState State
		{
			get
			{
				return this.Inner.State;
			}
		}
	}
}
