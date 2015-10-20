// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
﻿using Platform.Collections;

namespace Shaolinq.Persistence
{
	public struct InsertResults
	{
		public IReadOnlyList<DataAccessObject> ToFixUp { get; }
		public IReadOnlyList<DataAccessObject> ToRetry { get; }

		public InsertResults(IList<DataAccessObject> toFixUp, IList<DataAccessObject> toRetry)
			: this()
		{
			this.ToFixUp = new ReadOnlyList<DataAccessObject>(toFixUp);
			this.ToRetry = new ReadOnlyList<DataAccessObject>(toRetry);
		}
	}
}
