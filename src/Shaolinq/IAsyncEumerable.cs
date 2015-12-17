namespace Shaolinq
{
	public interface IAsyncEumerable<out T>
	{
		IAsyncEnumerator<T> GetAsyncEnumerator();
	}
}
