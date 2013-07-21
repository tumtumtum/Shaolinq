using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq.Tests.DataAccessModel.Basic
{
	public abstract class ObjectWithGuidAutoIncrementPrimaryKey
		: DataAccessObject<Guid>
	{	
	}
}
