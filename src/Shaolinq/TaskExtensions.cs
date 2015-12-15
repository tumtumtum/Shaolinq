// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Threading.Tasks;

namespace Shaolinq
{
	internal static class TaskExtensions
	{
		public static void AwaitResultOnAnyContext(this Task task)
		{
			task.ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public static T AwaitResultOnAnyContext<T>(this Task<T> task)
		{
			return task.ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
