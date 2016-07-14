#pragma warning disable
using System;
using System.Data;
using Shaolinq.Persistence;
using Npgsql;
using NpgsqlTypes;
using Platform;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Postgres
{
    public partial class PostgresSqlTransactionalCommandsContext
    {
        public Task CommitAsync()
        {
            return CommitAsync(CancellationToken.None);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            if (this.preparedTransactionName != null)
            {
                using (var command = this.CreateCommand())
                {
                    command.CommandText = $"COMMIT PREPARED '{this.preparedTransactionName}';";
                    command.ExecuteNonQueryEx(this.DataAccessModel);
                }
            }

            base.Commit();
        }

        public Task RollbackAsync()
        {
            return RollbackAsync(CancellationToken.None);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (this.preparedTransactionName != null)
            {
                using (var command = this.CreateCommand())
                {
                    command.CommandText = $"ROLLBACK PREPARED '{this.preparedTransactionName}';";
                    command.ExecuteNonQueryEx(this.DataAccessModel);
                }
            }

            base.Rollback();
        }
    }
}