// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using NUnit.Framework;
using Platform.Xml.Serialization;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ConfigurationTests
	{
		[Test]
		public void Test_LoadCustomConfig()
		{
			var config = DataAccessModel.GetConfiguration("TestDataAccessModelPostgresDotConnect");

			var s = XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(config);
		}
	}
}
