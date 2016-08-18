// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using Shaolinq.Logging;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteOfficialsSqlDatabaseContext
		: SqliteSqlDatabaseContext
	{
		public static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
		private static readonly Regex SqliteUriRegex = new Regex(@"file:(?<path>(:memory:)|([^\?]*))(?<query>\?.*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static string ConvertNewStyleUriToOldStyleUri(string uri, out bool isInMemory)
		{
			var match = SqliteUriRegex.Match(uri);

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
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDialect = new SqliteSqlDialect();
			var typeDescriptorProvider = model.TypeDescriptorProvider;
			var sqlDataTypeProvider = new SqliteSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, options => new SqliteSqlQueryFormatter(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider));

			return new SqliteOfficialsSqlDatabaseContext(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager);
		}

		public SqliteOfficialsSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, contextInfo, sqlDataTypeProvider, sqlQueryFormatterManager)
		{
			Version version;
			var versionString = SQLiteConnection.SQLiteVersion;

			if (Version.TryParse(versionString, out version))
			{
				if (version < new Version(3, 7, 7))
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

			var connectionStringBuilder = new SQLiteConnectionStringBuilder
			{
				FullUri = this.FileName,
				Enlist = false,
				ForeignKeys = true
			};
			
			this.ConnectionString = connectionStringBuilder.ConnectionString;
			this.ServerConnectionString = this.ConnectionString;

			this.SchemaManager = new SqliteOfficialSqlDatabaseSchemaManager(this);
		}

		public override void DropAllConnections()
		{
			SQLiteConnection.ClearAllPools();
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return new SQLiteFactory();
		}

		public override Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			// http://www.sqlite.org/c3ref/c_abort.html

			var sqliteException = exception as SQLiteException;

			if (sqliteException == null)
			{
				return base.DecorateException(exception, dataAccessObject, relatedQuery);
			}

			if (sqliteException.ErrorCode == SqliteErrorCodes.SqliteConstraint)
			{
				if (sqliteException.Message.IndexOf("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return new MissingRelatedDataAccessObjectException(null, dataAccessObject, sqliteException, relatedQuery);
				}
				else if (sqliteException.Message.IndexOf("NOT NULL", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return new MissingPropertyValueException(dataAccessObject, sqliteException, relatedQuery);
				}
				else
				{
					if (dataAccessObject != null)
					{
						var primaryKeyNames = dataAccessObject.GetAdvanced().TypeDescriptor.PrimaryKeyProperties.Select(c => c.DeclaringTypeDescriptor.PersistedName + "." + c.PersistedName);

						if (primaryKeyNames.Any(c => sqliteException.Message.IndexOf(c, StringComparison.Ordinal) >= 0))
						{
							return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
						}
					}

					return new UniqueConstraintException(exception, relatedQuery);
				}
			}

			return new DataAccessException(exception, relatedQuery);
		}
	}
}
