using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Shaolinq.Sqlite
{
	internal class SqliteRuntimeOfficialAssemblyManager
	{
		private static Assembly officialAssembly = null;

		public static void Init()
		{
			GetOfficialAssembly();
		}

		public static Assembly GetOfficialAssembly()
		{
			if (SqliteRuntimeOfficialAssemblyManager.officialAssembly == null)
			{
				lock (typeof(SqliteRuntimeOfficialAssemblyManager))
				{
					if (SqliteRuntimeOfficialAssemblyManager.officialAssembly == null)
					{
						Assembly loadedAssembly = null;

						var useEmbeddedAssembly = SqliteSqlDatabaseContext.IsRunningMono();

						if (useEmbeddedAssembly || true)
						{
							using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shaolinq.Sqlite.Embedded.System.Data.SQLite.Standard.dll.gz"))
							{
								using (var stream = new GZipStream(resourceStream, CompressionMode.Decompress))
								{
									var buffer = new byte[1024*32];

									using (var uncompressData = new MemoryStream())
									{
										int read;

										while ((read = stream.Read(buffer, 0, 1024)) > 0)
										{
											uncompressData.Write(buffer, 0, read);
										}

										loadedAssembly = Assembly.Load(uncompressData.ToArray());
									}
								}
							}
						}
						else
						{
							loadedAssembly = Assembly.Load("System.Data.SQLite");
						}

						var sqliteConnectionType = loadedAssembly.GetType("System.Data.SQLite.SQLiteConnection");
						var pathParam = Expression.Parameter(typeof(string));
						var createFileMethod = sqliteConnectionType.GetMethod("CreateFile", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);

						createFile = Expression.Lambda<Action<string>>(Expression.Call(null, createFileMethod, pathParam), pathParam).Compile();

						clearAllPools = Expression.Lambda<Action>(Expression.Call(null, sqliteConnectionType.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static))).Compile();

						sqliteExceptionType = loadedAssembly.GetType("System.Data.SQLite.SQLiteException");
						var exceptionParam = Expression.Parameter(typeof(Exception));
						getExceptionErrorCode = Expression.Lambda<Func<Exception, int>>(Expression.Property(Expression.Convert(exceptionParam, sqliteExceptionType), "ErrorCode"), exceptionParam).Compile();

						var sqliteFactoryType = loadedAssembly.GetType("System.Data.SQLite.SQLiteFactory");
						newDbProviderFactory = Expression.Lambda<Func<DbProviderFactory>>(Expression.New(sqliteFactoryType)).Compile();

						var connectionStringBuilderType = loadedAssembly.GetType("System.Data.SQLite.SQLiteConnectionStringBuilder");
						var uriParam = Expression.Parameter(typeof(string));
						var newExpression = Expression.New(connectionStringBuilderType);
						var bindings = new List<MemberBinding>();
						bindings.Add(Expression.Bind(connectionStringBuilderType.GetProperty("Enlist", BindingFlags.Instance | BindingFlags.Public), Expression.Constant(false)));
						bindings.Add(Expression.Bind(connectionStringBuilderType.GetProperty("ForeignKeys", BindingFlags.Instance | BindingFlags.Public), Expression.Constant(true)));
						bindings.Add(Expression.Bind(connectionStringBuilderType.GetProperty("FullUri", BindingFlags.Instance | BindingFlags.Public), uriParam));
						
						var memberInitExpression = Expression.MemberInit(newExpression, bindings.ToArray());
						var body = Expression.Property(memberInitExpression, "ConnectionString");

						SqliteRuntimeOfficialAssemblyManager.buildConnectionString = Expression.Lambda<Func<string, string>>(body, uriParam).Compile();

						Thread.MemoryBarrier();

						SqliteRuntimeOfficialAssemblyManager.officialAssembly = loadedAssembly;
					}
				}
			}

			return SqliteRuntimeOfficialAssemblyManager.officialAssembly;
		}

		private static Action clearAllPools;
		private static Type sqliteExceptionType; 
		private static Action<string> createFile;
		private static Func<Exception, int> getExceptionErrorCode;
		private static Func<string, string> buildConnectionString; 
		private static Func<DbProviderFactory> newDbProviderFactory;
		
		public static void CreateFile(string path)
		{
			SqliteRuntimeOfficialAssemblyManager.createFile(path);
		}

		public static void ClearAllPools()
		{
			SqliteRuntimeOfficialAssemblyManager.clearAllPools();
		}

		public static bool IsSqLiteExceptionType(Exception e)
		{
			return sqliteExceptionType.IsInstanceOfType(e);
		}

		public static int GetExceptionErrorCode(Exception e)
		{
			return SqliteRuntimeOfficialAssemblyManager.getExceptionErrorCode(e);
		}

		public static DbProviderFactory NewDbProviderFactory()
		{
			return SqliteRuntimeOfficialAssemblyManager.newDbProviderFactory();
		}

		public static string BuildConnectionString(string fullUri)
		{
			return SqliteRuntimeOfficialAssemblyManager.buildConnectionString(fullUri);
		}
	}
}
