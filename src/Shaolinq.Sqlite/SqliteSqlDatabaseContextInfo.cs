// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

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
				var useEmbeddedAssembly = SqliteSqlDatabaseContext.IsRunningMono();

				if (useEmbeddedAssembly)
				{
					if (Interlocked.CompareExchange(ref MonoLibraryAlreadyLoaded, 1, 0) == 0)
					{
						var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
						var assembly = assemblies.FirstOrDefault(c => c.GetName().Name == "System.Data.SQLite");

						if (assembly == null)
						{
							using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shaolinq.Sqlite.Embedded.System.Data.SQLite.Standard.dll.gz"))
							{
								using (var stream = new GZipStream(resourceStream, CompressionMode.Decompress))
								{
									var buffer = new byte[1024];

									using (var uncompressData = new MemoryStream())
									{
										int read;

										while ((read = stream.Read(buffer, 0, 1024)) > 0)
										{
											uncompressData.Write(buffer, 0, read);
										}

										// This totally relies on Mono currently loading all assemblies into the same "load context" 

										Assembly.Load(uncompressData.ToArray());
									}
								}
							}
						}
					}
				}

				var retval =  SqliteWindowsSqlDatabaseContext.Create(this, model);

				return retval;
			}
		}
	}
}
