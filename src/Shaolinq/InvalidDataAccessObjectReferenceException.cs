using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq
{
	/// <summary>
	/// Thrown when you try to use a deflated DAO reference to update an object but the deflated reference
	/// is invalid.
	/// </summary>
	public class InvalidDataAccessObjectReferenceException
		: DataAccessException
	{
	}
}
