// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿
namespace Shaolinq.Sqlite
{
	[XmlElement]
	public class SqliteSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }

		private static int MonoLibraryAlreadyLoaded;

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			var useMonoData = Environment.GetEnvironmentVariable("SHAOLINQ_USE_MONO_DATA_SQLITE");

			if (!string.IsNullOrEmpty(useMonoData) && SqliteSqlDatabaseContext.IsRunningMono())
			{
				return SqliteMonoSqlDatabaseContext.Create(this, model);
			}
			else
			{
				if (SqliteSqlDatabaseContext.IsRunningMono())
				{
					if (Interlocked.CompareExchange(ref MonoLibraryAlreadyLoaded, 1, 0) == 0)
					{
						var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
						var assembly = assemblies.FirstOrDefault(c => c.GetName().Name == "System.Data.SQLite");

						if (assembly == null)
						{
							byte[] bytes;
							var codeBase = new Uri(this.GetType().Assembly.CodeBase);

							if (string.Equals(codeBase.Scheme, "file", StringComparison.InvariantCultureIgnoreCase))
							{
								var fileName = Path.Combine(Path.GetDirectoryName(codeBase.LocalPath), "Mono", "System.Data.SQLite.Standard.dll");

								bytes = File.ReadAllBytes(fileName);
							}
							else
							{
								throw new NotSupportedException("Sqlite Provider requires application to be located on a local file system");
							}

							Assembly.Load(bytes);
						}
					}
				}

				return SqliteWindowsSqlDatabaseContext.Create(this, model);
			}
		}
	}
}
