using System;

namespace Shaolinq
{
	public abstract class IndexAttributeBase
		: Attribute
	{
		private bool? orderData;

		/// <summary>
		/// Indicates that this index or primary key should determine the how data is ordered when written.
		/// </summary>
		/// <remarks>
		/// If set to true, the table data is stored in order of the key or index. If unconfigured the underlying
		/// <c>RDBMS</c> will determine the value. <c>SQL Server</c> and <c>MySQL</c> will default to <c>true</c> for
		/// primary keys but not indexes (referred to as clustered indexes). All other RDBMS will tend to default to <c>false</c>
		/// and instead data is stored separately.
		/// </remarks>
		public bool OrganizationIndex
		{
			get { return this.orderData ?? false; } set { this.orderData = value; }
		}

		public bool OrganizationIndexExplicitlySpecified
		{
			get { return this.orderData != null; } set { if (!value) { this.orderData = null; } }
		}

		/// <summary>
		/// An integer representing the relative order of the current property in the index.
		/// Order is undefined if multiple properties have the same <c>IndexName</c> and <c>CompositeOrder</c>
		/// </summary>
		public int CompositeOrder { get; set; } = int.MinValue;

		/// <summary>
		/// The order of the index. Unspecified is database dependent but usually ascending.
		/// </summary>
		public SortOrder SortOrder { get; set; }
	}
}