// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;

namespace Shaolinq.Persistence
{
	public partial class DbCommandWrapper
		: IDbCommand
	{
		public IDbCommand Inner { get; }

		public DbCommandWrapper(IDbCommand inner)
		{
			this.Inner = inner;
		}

		public virtual void Dispose()
		{
			this.Inner.Dispose();
		}

		public virtual void Prepare()
		{
			this.Inner.Prepare();
		}

		public virtual void Cancel()
		{
			this.Inner.Cancel();
		}

		public virtual IDbDataParameter CreateParameter()
		{
			return this.Inner.CreateParameter();
		}
		
		public virtual int ExecuteNonQuery()
		{
			return this.Inner.ExecuteNonQuery();
		}

		public virtual IDataReader ExecuteReader()
		{
			return this.Inner.ExecuteReader();
		}
		
		public virtual IDataReader ExecuteReader(CommandBehavior behavior)
		{
			return this.Inner.ExecuteReader(behavior);
		}

		public virtual object ExecuteScalar()
		{
			return this.Inner.ExecuteScalar();
		}

		public virtual IDataParameterCollection Parameters => this.Inner.Parameters;
		public virtual IDbConnection Connection { get { return this.Inner.Connection; } set { this.Inner.Connection = value; } }
		public virtual IDbTransaction Transaction { get { return this.Inner.Transaction; } set { this.Inner.Transaction = value; } }
		public virtual string CommandText { get { return this.Inner.CommandText; } set { this.Inner.CommandText = value; } }
		public virtual int CommandTimeout { get { return this.Inner.CommandTimeout; } set { this.Inner.CommandTimeout = value; } }
		public virtual CommandType CommandType { get { return this.Inner.CommandType; } set { this.Inner.CommandType = value; } }
		public virtual UpdateRowSource UpdatedRowSource { get { return this.Inner.UpdatedRowSource; } set { this.Inner.UpdatedRowSource = value; } }
	}
}
