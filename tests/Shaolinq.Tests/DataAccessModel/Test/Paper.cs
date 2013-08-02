using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform;

namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class Paper
		: DataAccessObject<string>
	{
		public string PaperCode
		{
			get
			{
				return this.Id;
			}
			set
			{
				this.Id = value;
			}
		}

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Lecturer> Lecturers { get; }
	}
}
