// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.AsyncRewriter
{
	public interface IAsyncRewriterLogger
	{
		void LogWarning(string text);
		void LogError(string text);
		void LogMessage(string text);
	}
}
