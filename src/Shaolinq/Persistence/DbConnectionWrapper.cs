// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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

		public virtual void Dispose() => this.Inner.Dispose();
		public virtual IDbTransaction BeginTransaction() => this.Inner.BeginTransaction();
		public virtual IDbTransaction BeginTransaction(IsolationLevel il) => this.Inner.BeginTransaction(il);
		public virtual void Close() => this.Inner.Close();
		public virtual void ChangeDatabase(string databaseName) => this.Inner.ChangeDatabase(databaseName);
		public virtual IDbCommand CreateCommand() => this.Inner.CreateCommand();
		public virtual void Open() => this.Inner.Open();
		public virtual string Database => this.Inner.Database;
		public virtual ConnectionState State => this.Inner.State;
		public virtual int ConnectionTimeout => this.Inner.ConnectionTimeout;
		public virtual string ConnectionString { get { return this.Inner.ConnectionString; } set { this.Inner.ConnectionString = value; } }
	}
}
