// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class School
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		[ComputedTextMember("urn:$(TYPENAME_LOWER):{Id}")]
		public abstract string Urn { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Student> Students { get; }
	}
}
