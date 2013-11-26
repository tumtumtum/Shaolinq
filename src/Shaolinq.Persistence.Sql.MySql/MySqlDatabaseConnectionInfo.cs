// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql.MySql
{
	[XmlElement]
	public class MySqlDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
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
	}
}
