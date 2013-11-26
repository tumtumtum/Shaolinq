// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql
{
	[XmlElement]
	public class DatabaseConnectionInfo
	{
		[XmlAttribute]
		public PersistenceMode PersistenceMode { get; set; }

		[XmlAttribute]
		public int CommandTimeout { get; set; }

		[XmlAttribute]
		public int ConnectionTimeout { get; set; }

		[XmlAttribute]
		public string SchemaNamePrefix { get; set; }

		public DatabaseConnectionInfo()
		{
			this.CommandTimeout = 120;
			this.ConnectionTimeout = 60;
			this.SchemaNamePrefix = "";
			this.PersistenceMode = PersistenceMode.ReadWrite;
		}
	}
}
