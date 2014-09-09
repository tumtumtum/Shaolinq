using System;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	[XmlElement]
	public abstract class PostgresSharedSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string UserId { get; set; }

		[XmlAttribute]
		public int Port { get; set; }

		[XmlAttribute]
		public bool Pooling { get; set; }

		[XmlAttribute]
		public int MinPoolSize { get; set; }

		[XmlAttribute]
		public int MaxPoolSize { get; set; }

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool NativeUuids { get; set; }

		[XmlAttribute]
		public bool NativeEnums { get; set; }

		public const int DefaultPostgresPort = 5432;

		protected PostgresSharedSqlDatabaseContextInfo()
		{
			this.Port = DefaultPostgresPort;
			this.Pooling = true;
			this.MaxPoolSize = 50;
			this.NativeUuids = true;
			this.NativeEnums = false;
		}
	}
}
