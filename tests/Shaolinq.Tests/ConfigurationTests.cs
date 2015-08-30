// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
