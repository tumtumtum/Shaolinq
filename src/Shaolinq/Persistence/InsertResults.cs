// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence
{
	public struct InsertResults
	{
		public ReadOnlyCollection<DataAccessObject> ToFixUp { get; private set; }
		public ReadOnlyCollection<DataAccessObject> ToRetry { get; private set; }

		public InsertResults(IList<DataAccessObject> toFixUp, IList<DataAccessObject> toRetry)
			: this()
		{
			this.ToFixUp = new ReadOnlyCollection<DataAccessObject>(toFixUp);
			this.ToRetry = new ReadOnlyCollection<DataAccessObject>(toRetry);
		}
	}
}
