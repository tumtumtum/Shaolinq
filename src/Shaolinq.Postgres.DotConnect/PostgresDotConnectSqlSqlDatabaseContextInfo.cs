// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	[XmlElement]
	public class PostgresDotConnectSqlSqlDatabaseContextInfo
		: PostgresSharedSqlDatabaseContextInfo
	{
		[XmlAttribute]
		public bool UnpreparedExecute { get; set; }

		public PostgresDotConnectSqlSqlDatabaseContextInfo()
		{
			this.UnpreparedExecute = false;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresDotConnectSqlDatabaseContext.Create(this, model);
		}
	}
}
