// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public partial class MarsDbCommand
		: DbCommandWrapper
	{
		internal readonly SqlTransactionalCommandsContext context;

		public MarsDbCommand(SqlTransactionalCommandsContext context, IDbCommand inner)
			: base(inner)
		{
			this.context = context;
		}

		[RewriteAsync]
		public override int ExecuteNonQuery()
		{
			this.context.currentReader?.BufferAll();

			var dbCommand = this.Inner as DbCommand;

			if (dbCommand != null)
			{
				return dbCommand.ExecuteNonQuery();
			}
			else
			{
				return base.ExecuteNonQuery();
			}
		}

		[RewriteAsync]
		public override object ExecuteScalar()
		{
			this.context.currentReader?.BufferAll();

			var dbCommand = this.Inner as DbCommand;

			if (dbCommand != null)
			{
				return dbCommand.ExecuteScalar();
			}
			else
			{
				return base.ExecuteScalar();
			}
		}

		[RewriteAsync]
		public override IDataReader ExecuteReader()
		{
			this.context.currentReader?.BufferAll();

			var dbCommand = this.Inner as DbCommand;

			if (dbCommand != null)
			{
				return new MarsDataReader(this, dbCommand.ExecuteReader());
			}
			else
			{
				return new MarsDataReader(this, base.ExecuteReader());
			}
		}

		[RewriteAsync]
		public override IDataReader ExecuteReader(CommandBehavior behavior)
		{
			this.context.currentReader?.BufferAll();

			var dbCommand = this.Inner as DbCommand;

			if (dbCommand != null)
			{
				return new MarsDataReader(this, dbCommand.ExecuteReader(behavior));
			}
			else
			{
				return new MarsDataReader(this, base.ExecuteReader(behavior));
			}
		}
	}
}
