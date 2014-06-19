// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence
{
	public struct InsertResults
	{
		public ReadOnlyCollection<IDataAccessObject> ToFixUp { get; private set; }
		public ReadOnlyCollection<IDataAccessObject> ToRetry { get; private set; }

		public InsertResults(IList<IDataAccessObject> toFixUp, IList<IDataAccessObject> toRetry)
			: this()
		{
			this.ToFixUp = new ReadOnlyCollection<IDataAccessObject>(toFixUp);
			this.ToRetry = new ReadOnlyCollection<IDataAccessObject>(toRetry);
		}
	}
}
