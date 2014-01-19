// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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
		public PostgresSqlDatabaseContextInfo()
		{
			this.Port = 5432;
			this.Pooling = true;
			this.MaxPoolSize = 100;
			this.NativeUuids = true;
		}
		
		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return PostgresSqlDatabaseContext.Create(this, model);
		}
	}
}
