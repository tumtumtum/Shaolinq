namespace Shaolinq
{
	public static class StringExtensions
	{
		/// <summary>
		/// Used to support the SQL "Like" operation
		/// </summary>
		public static bool IsLike(this string stringValue, string value)
		{
			return stringValue.Contains(value);
		}
	}
}
