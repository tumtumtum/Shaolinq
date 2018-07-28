// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq.Persistence
{
	public struct InsertResults
	{
		public IReadOnlyList<DataAccessObject> ToFixUp { get; }
		public IReadOnlyList<DataAccessObject> ToRetry { get; }

		public InsertResults(IList<DataAccessObject> toFixUp, IList<DataAccessObject> toRetry)
			: this()
		{
			this.ToFixUp = toFixUp.ToReadOnlyCollection();
			this.ToRetry = toRetry.ToReadOnlyCollection();
		}
	}
}
