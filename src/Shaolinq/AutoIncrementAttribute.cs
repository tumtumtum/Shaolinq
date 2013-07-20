using System;

namespace Shaolinq
{
	/// <summary>
	/// Applied to a persistable property to indicate that each new object will have this property set to the next value in the sequence.
	/// </summary>
	/// <remarks>
	/// This property can also be applied on overridden properties to disable
	/// the autoincrement attribute (by setting <see cref="AutoIncrement"/>
	/// to false).
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property)]
	public class AutoIncrementAttribute
		: Attribute
	{
		public bool AutoIncrement { get; set; }

		public AutoIncrementAttribute()
			: this(true)    
		{
		}

		public AutoIncrementAttribute(bool autoIncrement)
		{
			this.AutoIncrement = autoIncrement;
		}
	}
}
