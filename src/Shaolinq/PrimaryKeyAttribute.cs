using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute
		: Attribute
	{
		public bool IsPrimaryKey { get; set; }

		public PrimaryKeyAttribute()
		{
			this.IsPrimaryKey = true;	
		}
	}
}
