// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
﻿using Platform.Collections;

namespace Shaolinq.Persistence
{
	public struct InsertResults
	{
		public IReadOnlyList<DataAccessObject> ToFixUp { get; private set; }
		public IReadOnlyList<DataAccessObject> ToRetry { get; private set; }

		public InsertResults(IList<DataAccessObject> toFixUp, IList<DataAccessObject> toRetry)
			: this()
		{
			this.ToFixUp = new ReadOnlyList<DataAccessObject>(toFixUp);
			this.ToRetry = new ReadOnlyList<DataAccessObject>(toRetry);
		}
	}
}
