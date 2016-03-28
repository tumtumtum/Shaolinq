using System.Threading;

namespace Shaolinq.Analytics
{
	internal class QueryAnalytics
		: IQueryAnalytics
	{
		private long queryCount;
		public long QueryCount => queryCount;

		internal void IncrementQueryCount()
		{
			Interlocked.Increment(ref queryCount);
		}
	}
}