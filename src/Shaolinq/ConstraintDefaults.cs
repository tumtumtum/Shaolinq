using System;
using Platform.Validation;
using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class ConstraintDefaults
	{
		public static readonly ConstraintDefaults Default = new ConstraintDefaults();

		[XmlAttribute]
		public int StringMaximumLength
		{
			get;
			set;
		}

		[XmlAttribute]
		public int StringPrimaryKeyMaximumLength
		{
			get;
			set;
		}

		[XmlAttribute]
		public SizeFlexibility StringSizeFlexibility
		{
			get;
			set;
		}

		public ConstraintDefaults()
		{
			this.StringMaximumLength = 256;
			this.StringPrimaryKeyMaximumLength = 64;
		}

		public ConstraintDefaults(ConstraintDefaults original)
		{
			this.StringMaximumLength = original.StringMaximumLength;
			this.StringPrimaryKeyMaximumLength = original.StringPrimaryKeyMaximumLength;
			this.StringSizeFlexibility = original.StringSizeFlexibility;
		}
	}
}
