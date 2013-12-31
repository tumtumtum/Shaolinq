// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	[XmlElement]
	public class MySqlDatabaseConnectionInfo
		: DatabaseConnectionInfo
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

		public MySqlDatabaseConnectionInfo()
		{
			this.PoolConnections = true;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext()
		{
			return new MySqlSqlDatabaseContext(this.ServerName, this.DatabaseName, this.UserName, this.Password, this.PoolConnections, this.SchemaName, this.TableNamePrefix, this.Categories);
		}
	}
}
