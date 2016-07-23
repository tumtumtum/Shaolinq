namespace Shaolinq.Postgres
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using Npgsql;
	using Platform;
	using Shaolinq;
	using NpgsqlTypes;
	using Shaolinq.Postgres;
	using Shaolinq.Persistence;

	public partial class PostgresSqlTransactionalCommandsContext
	{
		public override Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public override async Task CommitAsync(CancellationToken cancellationToken)
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"COMMIT PREPARED '{this.preparedTransactionName}';";
					await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false);
				}
			}

			await base.CommitAsync(cancellationToken).ConfigureAwait(false);
		}

		public override Task RollbackAsync()
		{
			return RollbackAsync(CancellationToken.None);
		}

		public override async Task RollbackAsync(CancellationToken cancellationToken)
		{
			if (this.preparedTransactionName != null)
			{
				using (var command = this.CreateCommand())
				{
					command.CommandText = $"ROLLBACK PREPARED '{this.preparedTransactionName}';";
					await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken).ConfigureAwait(false);
				}
			}

			await base.RollbackAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}