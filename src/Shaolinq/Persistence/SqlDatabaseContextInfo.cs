// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data.Common;
using Platform.Xml.Serialization;

namespace Shaolinq.Persistence
{
	[XmlElement]
	public abstract class SqlDatabaseContextInfo
	{
		public const int DefaultCommandTimeout = 120;
		public const int DefaultConnectionTimeout = 60;

		/// <summary>
		/// The connection string for this connection/context. This value can be <see langword="null"/> if enough
		/// property values are provided for the Shaolinq provider to create a connection string automatically.
		/// </summary>
		[XmlAttribute]
		public string ConnectionString { get; set; }

		/// <summary>
		/// A comma deliminated list of categories that this connection/context belongs to.
		/// </summary>
		/// <remarks>
		/// Categories are currently not used.
		/// </remarks>
		[XmlAttribute]
		public string Categories { get; set; } = "";

		/// <summary>
		/// The maximum execution time for each command being executed in seconds. If null the default value is <see cref="DefaultCommandTimeout"/> or 120 seconds.
		/// </summary>
		/// <remarks>
		/// Other factors may affect the timeout of an executing command such as how the server is configured.
		/// </remarks>
		[XmlAttribute]
		public int? CommandTimeout { get; set; }
		
		/// <summary>
		/// The idle timeout for the connection in seconds. If null the default value is <see cref="DefaultConnectionTimeout"/> or 60 seconds.
		/// </summary>
		/// <remarks>
		/// This value is configured by setting <see cref="DbCommand.CommandTimeout"/> with each new command.
		/// </remarks>
		[XmlAttribute]
		public int? ConnectionTimeout { get; set; }

		/// <summary>
		/// The idle timeout for each connection as configured on the DbConnection or connection string.
		/// </summary>
		/// <remarks>
		/// This value is the value configured once on the connection string or <see cref="DbConnection"/>
		/// if supported by the underlying ADO.NET provider. This value may or may not have the same semantics
		/// as <see cref="ConnectionTimeout"/> and is entirely dependent on the underlying ADO.NET provider.
		/// </remarks>
		[XmlAttribute]
		public int? ConnectionCommandTimeout { get; set; }

		/// <summary>
		/// A prefix to put before each table name. This is a global setting for the connection and overrides
		/// any <see cref="NamingTransformsConfiguration"/>.
		/// </summary>
		/// <remarks>
		/// It is preferred to use <see cref="NamingTransformsConfiguration.DataAccessObjectName"/> as you will
		/// have more flexibility by using regex to control the naming conventions.
		/// </remarks>
		[XmlAttribute]
		public string TableNamePrefix { get; set; } = "";

		/// <summary>
		/// If true then automatically generated index names should include the names of included (non indexed) columnss. Default is false.
		/// </summary>
		[XmlAttribute]
		public bool IndexNamesShouldIncludeIncludedProperties { get; set; }

		/// <summary>
		/// The name of the schema to use for the database.
		/// </summary>
		[XmlAttribute]
		public string SchemaName { get; set; } = "";
		
		/// <summary>
		/// The type of the <see cref="SqlDataTypeProvider"/> for this connection/context.
		/// </summary>
		[XmlAttribute]
		public Type SqlDataTypeProvider { get; set; }

		[XmlElement]
		[XmlListElement("Type", ItemType = typeof(Type), SerializeAsValueNode = true, ValueNodeAttributeName = "Name")]
		public List<Type> SqlDataTypes { get; set; }

		public abstract SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model);
	}
}
