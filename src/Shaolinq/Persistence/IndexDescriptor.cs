using System.Collections.Generic;

namespace Shaolinq.Persistence
{
	public class IndexDescriptor
	{
		public string Name
		{
			get;
			set;
		}

		public bool IsUnique
		{
			get;
			set;
		}

		public bool AlsoIndexToLower
		{
			get; set;
		}

		public List<PropertyDescriptor> Properties
		{
			get;
			set;
		}

		public IndexDescriptor()
		{
			this.Properties = new List<PropertyDescriptor>();
		}
	}
}
