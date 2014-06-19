using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ConfigurationTests
	{
		[Test]
		public void Test_LoadCustomConfig()
		{
			DataAccessModel.GetConfiguration("TestDataAccessModelPostgresDotConnect");
		}
	}
}
