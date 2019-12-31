// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	[XmlElement]
	public class SqlServerSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		/// <summary>
		/// The name of the Database. Equivalent to <c>Database</c> or <c>Initial Catalog</c> in the connection string.
		/// </summary>
		[XmlAttribute]
		public string DatabaseName { get; set; }

		/// <summary>
		/// The name of the server or host. Equivalent to <c>Data Source</c> in the connection string.
		/// </summary>
		[XmlAttribute]
		public string ServerName { get; set; }

		/// <summary>
		/// The name of the instance on the server. Equivalent to the portion of the <see cref="ServerName"/> after a backslash.
		/// </summary>
		/// <remarks>
		/// Setting <see cref="ServerName"/> to <c>LOCALHOST</c> and this property to <c>SQLEXPRESS</c> would be equivalent to using a connection string with <c>"SERVER=LOCALHOST\SQLEXPRESS"</c>.
		/// </remarks>
		[XmlAttribute]
		public string Instance { get; set; }

		/// <summary>
		/// The username for the connection.
		/// </summary>[XmlAttribute]
		public string UserName { get; set; }

		/// <summary>
		/// The password for the connection.
		/// </summary>
		[XmlAttribute]
		public string Password { get; set; }

		/// <summary>
		/// Set to true to encrypt the connection.
		/// </summary>
		[XmlAttribute]
		public bool Encrypt { get; set; }

		/// <summary>
		/// Set to true to use Windows Authentication. Equivalent to <c>Trusted Connection</c> in the connection string.
		/// </summary>
		[XmlAttribute]
		public bool TrustedConnection { get; set; } = false;

		/// <summary>
		/// When true, explicitly specified index condition overrides the conditions implicitly added
		/// by <see cref="SqlServerUniqueNullIndexAnsiComplianceFixer"/>
		/// </summary>
		/// <remarks>
		/// This property defaults to false
		/// </remarks>
		[XmlAttribute]
		public bool ExplicitIndexConditionOverridesNullAnsiCompliance { get; set; }

		/// <summary>
		/// Determines whether deleting an existing database via <see cref="DataAccessModel.Create(DatabaseCreationOptions)"/>
		/// should only drop the tables rather than the entire database.
		/// </summary>
		/// <remarks>
		/// Only dropping tables rather than deleting the entire database is useful for database servers where you do
		/// not have permission to delete or create new databases (Azure, Virtual Hosting, etc)
		/// </remarks>
		[XmlAttribute]
		public bool DeleteDatabaseDropsTablesOnly { get; set; } = false;

		/// <summary>
		/// True if multiple active result sets per connection are allowed (default is true)
		/// </summary>
		/// <remarks>
		/// Allowing multiple active result sets allows queries to be performed whilst still iterating
		/// a result set without having to create a new connection/transaction.
		/// </remarks>
		[XmlAttribute]
		public bool MultipleActiveResultSets { get; set; } = true;

		/// <summary>
		/// True if Pooling is enabled (default is true)
		/// </summary>
		[XmlAttribute]
		public bool Pooling { get; set; } = true;
		
		/// <summary>
		/// Enables <see cref="DataAccessIsolationLevel.Snapshot"/> isolation.
		/// </summary>
		/// <remarks>
		/// This property only has an affect when the database is first created via
		/// <see cref="DataAccessModel.Create(DatabaseCreationOptions)"/>
		/// </remarks>
		[XmlAttribute]
		public bool AllowSnapshotIsolation { get; set; } = false;

		/// <summary>
		/// Makes the <see cref="DataAccessIsolationLevel.ReadCommitted"/> snapshop level act like
		/// <see cref="DataAccessIsolationLevel.Snapshot"/>
		/// </summary>
		[XmlAttribute]
		public bool ReadCommittedSnapshot { get; set; } = false;

		/// <summary>
		/// When <c>true</c> indexes will always exclude any row with null columns. When false (default)
		/// rows with nulls will only be excluded if the entire index is declared as unique.
		/// </summary>
		[XmlAttribute]
		public bool UniqueNullIndexAnsiComplianceFixerClassicBehaviour { get; set; } = false;

		/// <summary>
		/// The version of the SQL server type system to use
		/// </summary>
		/// <remarks>
		/// Possible valid values: 'SQL Server 2000', 'SQL Server 2005', 'SQL Server 2008', 'SQL Server 2012' (default if not specified is 2012)
		/// </remarks>
		[XmlAttribute]
		public string TypeSystemVersion { get; set; } = null;

		/// <summary>
		/// When <c>true</c> GUID types will be stored in the database with the UNIQUEIDENTIFIER sql type.
		/// When <c>false</c> (default) they will be stored as CHAR(32).
		/// </summary>
		[XmlAttribute]
		public bool NativeGuids { get; set; } = false;

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return SqlServerSqlDatabaseContext.Create(this, model);
		}
	}
}
