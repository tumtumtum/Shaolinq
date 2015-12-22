using System.Data;

namespace Shaolinq.Persistence
{
	public class MarsDbCommand
		: DbCommandWrapper
	{
		internal readonly SqlTransactionalCommandsContext context;

		public MarsDbCommand(SqlTransactionalCommandsContext context, IDbCommand inner)
			: base(inner)
		{
			this.context = context;
		}

		public override int ExecuteNonQuery()
		{
			this.context.currentReader?.BufferAll();

			return base.ExecuteNonQuery();
		}

		public override object ExecuteScalar()
		{
			this.context.currentReader?.BufferAll();
			
			return base.ExecuteScalar();
		}

		public override IDataReader ExecuteReader()
		{
			this.context.currentReader?.BufferAll();

			return new MarsDataReader(this, base.ExecuteReader());
		}

		public override IDataReader ExecuteReader(CommandBehavior behavior)
		{
			return new MarsDataReader(this, base.ExecuteReader(behavior));
		}
	}
}
