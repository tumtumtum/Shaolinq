using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Lecture
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract Paper Paper { get; set; }
	}
}