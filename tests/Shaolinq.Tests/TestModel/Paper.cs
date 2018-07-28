// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Paper
		: DataAccessObject<string>
	{
		[SizeConstraint(MaximumLength = 32)]
		public abstract override string Id { get; set; }

		[PersistedMember]
		public abstract int Points { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract int ExtraPoints1 { get; set; }

		[PersistedMember, DefaultValue(10)]
		public abstract int ExtraPoints2 { get; set; }

		[PersistedMember]
		public abstract int? MaximumClassSize { get; set; }
		
		[BackReference]
		public abstract Lecturer Lecturer { get; set; }

		public string PaperCode { get { return this.Id; } set { this.Id = value; } }
	}
}
