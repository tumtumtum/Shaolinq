// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	[XmlElement]
	public class PostgresDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName{ get; set; }

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
		public DateTimeKind DateTimeKindIfUnspecified { get; set; }

		public PostgresDatabaseContextInfo()
		{
			this.Port = 5432;
			this.Pooling = true;
			this.MaxPoolSize = 100;
			this.NativeUuids = true;
		}
		
		public override SqlDatabaseContext CreateSqlDatabaseContext(ConstraintDefaults constraintDefaults)
		{
			return PostgresSqlDatabaseContext.Create(this, constraintDefaults);
		}
	}
}
