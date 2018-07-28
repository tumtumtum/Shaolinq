// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Platform.Xml.Serialization;

namespace Shaolinq
{
	/// <summary>
	/// Used to configure the default constraints, if unspecified on persisted members, for string datatypes.
	/// </summary>
	[XmlElement]
	public class ConstraintDefaultsConfiguration
		: ICloneable
	{
		public const int DefaultStringMaximumLength = 512;
		public const int DefaultIndexedStringMaximumLength = 512;
		public const SizeFlexibility DefaultStringSizeFlexibility = SizeFlexibility.Variable;

		/// <summary>
		/// The maximum size of the string.
		/// </summary>
		[XmlAttribute]
		public int StringMaximumLength { get; set; } = DefaultStringMaximumLength;

		/// <summary>
		/// The maximum size of the string if the column is used in any index. If in doubt set as the same value for <see cref="StringMaximumLength"/>.
		/// </summary>
		[XmlAttribute]
		public int IndexedStringMaximumLength { get; set; } = DefaultIndexedStringMaximumLength;

		/// <summary>
		/// Determines how flexible the storage is for the string.
		/// </summary>
		/// <remarks>
		/// <see cref="SizeFlexibility.Variable"/> usually maps to <c>VARCHAR</c> and <see cref="SizeFlexibility.Fixed"/> maps to <c>CHAR</c>.
		/// </remarks>
		[XmlAttribute]
		public SizeFlexibility StringSizeFlexibility { get; set; } = DefaultStringSizeFlexibility;

		public ConstraintDefaultsConfiguration()
		{
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
