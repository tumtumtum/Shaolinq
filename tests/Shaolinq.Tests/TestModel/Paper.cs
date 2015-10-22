// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class Paper
		: DataAccessObject<string>
	{
		[SizeConstraint(MaximumLength=32)]
		public abstract override string Id
		{
			get;
			set;
		}

		public string PaperCode
		{
			get
			{
				return this.Id;
			}
			set
			{
				this.Id = value;
			}
		}

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Lecturer> Lecturers { get; }
	}
}
