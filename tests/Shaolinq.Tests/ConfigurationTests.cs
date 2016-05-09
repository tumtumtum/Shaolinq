// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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

			XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(config);
		}
	}
}
