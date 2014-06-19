// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

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

		public PostgresDotConnectSqlDatabaseContextInfo()
		{
			this.UnpreparedExecute = false;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresDotConnectSqlDatabaseContext.Create(this, model);
		}
	}
}
