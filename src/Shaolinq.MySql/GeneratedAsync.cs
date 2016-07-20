namespace Shaolinq.MySql
{
#pragma warning disable
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Text.RegularExpressions;
    using Shaolinq.Persistence;
    using System.Threading;
    using System.Threading.Tasks;
    using Shaolinq;
    using Shaolinq.MySql;
    using global::MySql.Data.MySqlClient;

    public partial class MySqlSqlDatabaseContext
    {
        public override Task<IDbConnection> OpenConnectionAsync()
        {
            return OpenConnectionAsync(CancellationToken.None);
        }

        public override async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            var retval = (await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
            using (var command = retval.CreateCommand())
            {
                var prefix = this.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
                var parameter = command.CreateParameter();
                parameter.DbType = DbType.String;
                parameter.ParameterName = $"{prefix}param";
                parameter.Value = this.SqlMode ?? "STRICT_ALL_TABLES";
                command.CommandText = $"SET SESSION sql_mode = {prefix}param;";
                command.Parameters.Add(parameter);
                await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
            }

            return retval;
        }
    }
}