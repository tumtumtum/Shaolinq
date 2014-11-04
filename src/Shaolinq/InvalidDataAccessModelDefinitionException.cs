using System;

namespace Shaolinq
{
	public class InvalidDataAccessModelDefinitionException
		: Exception
	{
		public InvalidDataAccessModelDefinitionException()
		{
		}

		public InvalidDataAccessModelDefinitionException(string message)
			: base(message)
		{
		}
	}
}
