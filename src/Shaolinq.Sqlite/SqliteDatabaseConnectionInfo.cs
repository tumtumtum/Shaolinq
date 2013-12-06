// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	[XmlElement]
	public class SqliteDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }

		public override DatabaseConnection CreateDatabaseConnection()
		{
			return new SqliteDatabaseConnection(this.FileName, this.SchemaNamePrefix);
		}
	}
}
