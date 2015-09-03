// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	[XmlElement]
	public class PostgresSqlDatabaseContextInfo
		: PostgresSharedSqlDatabaseContextInfo
	{
		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresSqlDatabaseContext.Create(this, model);
		}
	}
}
