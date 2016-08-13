namespace Shaolinq.Sqlite
{
#pragma warning disable
	using System;
	using System.IO;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Text.RegularExpressions;
	using Shaolinq;
	using Shaolinq.Sqlite;
	using Shaolinq.Persistence;

	public abstract partial class SqliteSqlDatabaseContext
	{
		public override Task<IDbConnection> OpenConnectionAsync()
		{
			return OpenConnectionAsync(CancellationToken.None);
		}

		public override async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			var retval = (await this.PrivateOpenConnectionAsync(cancellationToken).ConfigureAwait(false));
			if (retval == null)
			{
				return null;
			}

			using (var command = retval.CreateCommand())
			{
				command.CommandText = "PRAGMA foreign_keys = ON;";
				await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
			}

			return retval;
		}

		private Task<IDbConnection> PrivateOpenConnectionAsync()
		{
			return PrivateOpenConnectionAsync(CancellationToken.None);
		}

		private async Task<IDbConnection> PrivateOpenConnectionAsync(CancellationToken cancellationToken)
		{
			if (!this.IsInMemoryConnection)
			{
				return await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
			}

			if (this.IsSharedCacheConnection)
			{
				return await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
			}

			return this.connection ?? (this.connection = new SqlitePersistentDbConnection((await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))));
		}
	}
}