// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using log4net;

namespace Shaolinq.Sqlite
{
	public class SqliteOfficialsSqlDatabaseContext
		: SqliteSqlDatabaseContext
	{
		public static readonly ILog Logger = LogManager.GetLogger(typeof(Sql92QueryFormatter));
		private static readonly Regex sqliteUriRegex = new Regex(@"file:(?<path>(:memory:)|([^\?]*))(?<query>\?.*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static string ConvertNewStyleUriToOldStyleUri(string uri, out bool isInMemory)
		{
			var match = sqliteUriRegex.Match(uri);

			if (match.Success)
			{
				var path = match.Groups["path"].Value;
				var query = match.Groups["query"].Value;

				var queryParams = GetQueryParams(query);

				string value;
				var shared = false;
				var memory = false;
				var memoryByPath = false;

				memoryByPath = memory = String.Equals(path, ":memory:", StringComparison.InvariantCultureIgnoreCase);

				if (!memory && queryParams.TryGetValue("mode", out value))
				{
					memory = string.Equals(value, "memory", StringComparison.InvariantCultureIgnoreCase);
				}

				if (queryParams.TryGetValue("cache", out value))
				{
					shared = string.Equals(value, "shared", StringComparison.InvariantCultureIgnoreCase);
				}

				if (memory)
				{
					if (shared)
					{
						isInMemory = false;

						if (memoryByPath)
						{
							uri = "shaolinq_mem.db";
						}
						else
						{
							uri = "shaolinq_mem_" + path + ".db";
						}
					}
					else
					{
						isInMemory = true;

						uri = ":memory:";
					}
				}
				else
				{
					isInMemory = false;

					uri = path;
				}
			}
			else
			{
				isInMemory = string.Equals(uri.Trim(), ":memory:", StringComparison.InvariantCultureIgnoreCase);
			}

			return uri;
		}

		static Dictionary<string, string> GetQueryParams(string uri)
		{
			var matches = Regex.Matches(uri, @"[\?&](([^&=]+)=([^&=#]*))", RegexOptions.Compiled);
			return matches.Cast<Match>().ToDictionary(
				m => Uri.UnescapeDataString(m.Groups[2].Value),
				m => Uri.UnescapeDataString(m.Groups[3].Value), StringComparer.CurrentCultureIgnoreCase);
		}

		public static SqliteSqlDatabaseContext Create(SqliteSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqliteSqlDialect.Default, sqlDataTypeProvider, typeof(SqliteSqlQueryFormatter));

			return new SqliteOfficialsSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		public SqliteOfficialsSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager)
		{
			Version version;
			var versionString = SqliteRuntimeOfficialAssemblyManager.GetSqliteVersion();

			if (Version.TryParse(versionString, out version))
			{
				if (version.Major < 3 && version.Minor < 7 && version.MinorRevision < 7)
				{
					bool isInMemory;
					var uri = contextInfo.FileName;

					Logger.WarnFormat("Sqlite version {0} does not support URIs", versionString);

					uri = ConvertNewStyleUriToOldStyleUri(uri, out isInMemory);

					this.IsInMemoryConnection = isInMemory;
					this.IsSharedCacheConnection = false;
					this.FileName = uri;
				}
			}
			else
			{
				Logger.WarnFormat("Cannot parse sqlite version: {0}", versionString);
			}

			this.ConnectionString = SqliteRuntimeOfficialAssemblyManager.BuildConnectionString(this.FileName);
			this.ServerConnectionString = this.ConnectionString;
			this.SchemaManager = new SqliteOfficialSqlDatabaseSchemaManager(this);
		}

		public override void DropAllConnections()
		{
			SqliteRuntimeOfficialAssemblyManager.ClearAllPools();
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return SqliteRuntimeOfficialAssemblyManager.NewDbProviderFactory();
		}

		public override Exception DecorateException(Exception exception, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			if (!SqliteRuntimeOfficialAssemblyManager.IsSqLiteExceptionType(exception))
			{	
				return base.DecorateException(exception, relatedQuery);
			}

			if (SqliteRuntimeOfficialAssemblyManager.GetExceptionErrorCode(exception) == SqliteErrorCodes.SqliteConstraint)
			{
				return new UniqueKeyConstraintException(exception, relatedQuery);
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
