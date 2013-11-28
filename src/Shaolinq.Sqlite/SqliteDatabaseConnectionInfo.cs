// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	[XmlElement]
	public class SqliteDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }
	}
}
