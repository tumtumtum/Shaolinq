// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using Platform.Xml.Serialization;

namespace Shaolinq.Persistence
{
	[XmlElement]
	public abstract class SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string ConnectionString { get; set; }

		[XmlAttribute]
		public string Categories { get; set; }

		[XmlAttribute]
		public int? ConnectionCommandTimeout { get; set; }
		
		[XmlAttribute]
		public int? CommandTimeout { get; set; }
		
		[XmlAttribute]
		public int? ConnectionTimeout { get; set; }

		[XmlAttribute]
		public string TableNamePrefix { get; set; }
		
		[XmlAttribute]
		public string SchemaName { get; set; }
		
		[XmlAttribute]
		public Type SqlDataTypeProvider { get; set; }

		[XmlElement]
		[XmlListElement("Type", ItemType = typeof(Type), SerializeAsValueNode = true, ValueNodeAttributeName = "Name")]
		public List<Type> SqlDataTypes { get; set; }

		public const int DefaultCommandTimeout = 120;
		public const int DefaultConnectionTimeout = 60;

		protected SqlDatabaseContextInfo()
		{
			this.SchemaName = ""; 
			this.TableNamePrefix = "";
			this.Categories = "";
		}

		public abstract SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model);
	}
}
