// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres.DotConnect
{
	[XmlElement]
	public class PostgresDotConnectSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		public const bool DefaultPooling = true;
		public const int DefaultPostgresPort = 5432;
		public const bool DefaultUnpreparedExecute = true;

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
		public int MaxPoolSize { get; set; } = 50;

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool NativeUuids { get; set; } = true;

		[XmlAttribute]
		public bool NativeEnums { get; set; } = false;

		[XmlAttribute]
		public bool UnpreparedExecute { get; set; } = false;

		[XmlAttribute]
		public int KeepAlive { get; set; } = 3;

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresDotConnectSqlDatabaseContext.Create(this, model);
		}
	}
}
