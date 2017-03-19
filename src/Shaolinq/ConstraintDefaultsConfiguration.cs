// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class ConstraintDefaultsConfiguration
		: ICloneable
	{
		[XmlAttribute] public int StringMaximumLength { get; set; }
		[XmlAttribute] public int IndexedStringMaximumLength { get; set; }
		[XmlAttribute] public SizeFlexibility StringSizeFlexibility { get; set; }

		public ConstraintDefaultsConfiguration()
		{
			this.StringMaximumLength = 512;
			this.IndexedStringMaximumLength = 512;
		}

		public ConstraintDefaultsConfiguration(ConstraintDefaultsConfiguration original)
		{
			this.StringMaximumLength = original.StringMaximumLength;
			this.IndexedStringMaximumLength = original.IndexedStringMaximumLength;
			this.StringSizeFlexibility = original.StringSizeFlexibility;
		}
		
		public virtual object Clone() => new ConstraintDefaultsConfiguration(this);
	}
}
