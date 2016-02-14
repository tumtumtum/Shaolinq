// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Shaolinq
{
	public static class TaskExtensions
	{
		public static ConfiguredTaskAwaitable ContinueOnAnyContext(this Task task)
		{
			return task.ConfigureAwait(false);
		}

		public static ConfiguredTaskAwaitable<T> ContinueOnAnyContext<T>(this Task<T> task)
		{
			return task.ConfigureAwait(false);
		}
	}
}
