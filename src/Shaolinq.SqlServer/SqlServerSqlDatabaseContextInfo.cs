// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	[XmlElement]
	public class SqlServerSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string Instance { get; set; }

		[XmlAttribute]
		public string UserName { get; set; }

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool Encrypt { get; set; }

		[XmlAttribute]
		public bool TrustedConnection { get; set; } = false;

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

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return SqlServerSqlDatabaseContext.Create(this, model);
		}
	}
}
