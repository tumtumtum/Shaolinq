// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql.Postgres
{
	[XmlElement]
	public class PostgresPersistenceContextInfo
		: PersistenceContextInfo
	{
		[XmlAttribute]
		public string DatabaseName;

		public override string PersistenceContextName
		{
			get { return this.DatabaseName; }
			set { this.DatabaseName = value; }
		}

		[XmlElement("DatabaseConnections")]
		[XmlListElement(typeof(PostgresDatabaseConnectionInfo), "PostgresDatabaseConnection")]
		public PostgresDatabaseConnectionInfo[] DatabaseConnectionInfos
		{
			get;
			set;
		}

		public override PersistenceContextProvider NewDatabaseContextProvider()
		{
			return new PostgresPersistenceContextProvider(this.ContextName, this.DatabaseName, this.DatabaseConnectionInfos);
		}
	}
}
