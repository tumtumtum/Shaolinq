// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Microsoft.Build.Utilities;

namespace Shaolinq.AsyncRewriter
{
	public class AsyncRewriterTaskLogger: IAsyncRewriterLogger
	{
		private readonly TaskLoggingHelper log;

		public AsyncRewriterTaskLogger(TaskLoggingHelper log)
		{
			this.log = log;
		}

		public void LogWarning(string text)
		{
			this.log.LogWarning(text);
		}

		public void LogError(string text)
		{
			this.log.LogError(text);
		}

		public void LogMessage(string text)
		{
			this.log.LogMessage(text);
		}
	}
}