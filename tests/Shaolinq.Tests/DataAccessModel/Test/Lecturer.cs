// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Lecturer
		: DataAccessObject<Guid>
	{
		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Paper> Papers { get; }
	}
}
