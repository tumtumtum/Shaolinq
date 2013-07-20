namespace Shaolinq
{
	public struct DataAccessModelPersistenceContextInfo
	{
		public string ContextName
		{
			get;
			private set;
		}
		
		public DataAccessModelPersistenceContextInfo(string name)
			: this()
		{
			this.ContextName = name;
		}
	}
}
