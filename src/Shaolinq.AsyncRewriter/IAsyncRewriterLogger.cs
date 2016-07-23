namespace Shaolinq.AsyncRewriter
{
	public interface IAsyncRewriterLogger
	{
		void LogWarning(string text);
		void LogError(string text);
		void LogMessage(string text);
	}
}
