// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	/// <summary>
	/// See http://www.npgsql.org/doc/connection-string-parameters.html
	/// </summary>
	[XmlElement]
	public class PostgresSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		public const bool DefaultPooling = true;
		public const int DefaultPostgresPort = 5432;

		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string UserId { get; set; }

		[XmlAttribute]
		public int Port { get; set; } = DefaultPostgresPort;

		[XmlAttribute]
		public bool Pooling { get; set; } = DefaultPooling;

		[XmlAttribute]
		public int MinPoolSize { get; set; }

		[XmlAttribute]
		public int MaxPoolSize { get; set; } = 20;

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool NativeUuids { get; set; } = true;

		[XmlAttribute]
		public bool NativeEnums { get; set; } = false;

		[Obsolete]
		[XmlAttribute]
		public bool BackendTimeouts { get; set; } = true;
		
		[XmlAttribute]
		public int KeepAlive { get; set; } = 3;

		[XmlAttribute]
		public int ConnectionIdleLifetime { get; set; } = 3;

		[XmlAttribute]
		public int ConnectionPruningInterval { get; set; } = 3;
		
		[XmlAttribute]
		public bool EnablePreparedTransactions { get; set; } = false;

		[XmlAttribute]
		public bool ConvertInfinityDateTime { get; set; } = false;

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresSqlDatabaseContext.Create(this, model);
		}
	}
}
