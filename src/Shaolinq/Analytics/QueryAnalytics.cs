// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Threading;

namespace Shaolinq.Analytics
{
	internal class QueryAnalytics
		: IQueryAnalytics
	{
		private long queryCount;
		public long QueryCount => this.queryCount;

		internal void IncrementQueryCount()
		{
			Interlocked.Increment(ref this.queryCount);
		}
	}
}