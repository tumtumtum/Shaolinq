namespace Shaolinq
{
	public class InvalidPrimaryKeyPropertyAccessException
		: InvalidPropertyAccessException
	{
		public InvalidPrimaryKeyPropertyAccessException(string propertyName)
			: base(propertyName)
		{
		}
	}
}
