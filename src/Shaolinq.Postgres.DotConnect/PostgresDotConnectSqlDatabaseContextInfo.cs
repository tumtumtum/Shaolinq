// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	[XmlElement]
	public class PostgresDotConnectSqlDatabaseContextInfo
		: PostgresSharedSqlDatabaseContextInfo
	{
		[XmlAttribute]
		public bool UnpreparedExecute { get; set; }

		public const bool DefaultUnpreparedExecute = false;

		public PostgresDotConnectSqlDatabaseContextInfo()
		{
			this.UnpreparedExecute = DefaultUnpreparedExecute;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresDotConnectSqlDatabaseContext.Create(this, model);
		}
	}
}
