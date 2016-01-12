// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Paper
		: DataAccessObject<string>
	{
		[SizeConstraint(MaximumLength = 32)]
		public abstract override string Id { get; set; }

		[BackReference]
		public abstract Lecturer Lecturer { get; set; }

		public string PaperCode { get { return this.Id; } set { this.Id = value; } }
	}
}
