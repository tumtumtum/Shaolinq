// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[Flags]
	public enum LoadOptions
	{
		/// <summary>
		/// Return the items that were eager loaded (cached). Throws <see cref="InvalidOperationException"/> if the collection hasn't been eager loaded.
		/// </summary>
		EagerOnly = 1,

		/// <summary>
		/// Loads, caches and returns the items using a query.
		/// </summary>
		LazyOnly = 2,

		/// <summary>
		/// Return the items that were eager loaded or loads, caches and returns tems using a new query.
		/// </summary>
		EagerOrLazy = EagerOnly | LazyOnly
	}
}