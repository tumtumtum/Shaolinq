// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
		public int IndexedStringMaximumLength
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
			this.StringMaximumLength = 512;
			this.IndexedStringMaximumLength = 255;
		}

		public ConstraintDefaults(ConstraintDefaults original)
		{
			this.StringMaximumLength = original.StringMaximumLength;
			this.IndexedStringMaximumLength = original.IndexedStringMaximumLength;
			this.StringSizeFlexibility = original.StringSizeFlexibility;
		}
	}
}
