// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	[XmlElement]
	public class MySqlSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string UserName { get; set; }

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool PoolConnections { get; set; }

		public MySqlSqlDatabaseContextInfo()
		{
			this.PoolConnections = true;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext(ConstraintDefaults constraintDefaults)
		{
			return MySqlSqlDatabaseContext.Create(this, constraintDefaults);
		}
	}
}
