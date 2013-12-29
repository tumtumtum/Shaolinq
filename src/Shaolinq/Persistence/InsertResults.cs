// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence
{
	public class InsertResults
	{
		public ReadOnlyCollection<IDataAccessObject> ToFixUp { get; private set; }
		public ReadOnlyCollection<IDataAccessObject> ToRetry { get; private set; }

		public InsertResults(IList<IDataAccessObject> toFixUp, IList<IDataAccessObject> toRetry)
		{
			this.ToFixUp = new ReadOnlyCollection<IDataAccessObject>(toFixUp);
			this.ToRetry = new ReadOnlyCollection<IDataAccessObject>(toRetry);
		}
	}
}
