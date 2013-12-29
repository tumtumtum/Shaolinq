// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿
namespace Shaolinq.Sqlite
{
	[XmlElement]
	public class SqliteDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }

		public override SqlDatabaseContext CreateSqlDatabaseContext()
		{
			return new SqliteSqlDatabaseContext(this.FileName, this.SchemaNamePrefix);
		}
	}
}
