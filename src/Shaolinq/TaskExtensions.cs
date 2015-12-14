using System.Threading.Tasks;

namespace Shaolinq
{
	internal static class TaskExtensions
	{
		public static void StartAndOrGetResult(this Task task)
		{
			switch (task.Status)
			{
			case TaskStatus.Created:
				task.Start();
				break;
			case TaskStatus.RanToCompletion:
				return;
			}
			
			task.GetAwaiter().GetResult();
		}

		public static T StartAndOrGetResult<T>(this Task<T> task)
		{
			switch (task.Status)
			{
			case TaskStatus.Created:
				task.Start();
				break;
			case TaskStatus.RanToCompletion:
				return task.Result;
			}

			return task.GetAwaiter().GetResult();
		}
	}
}
