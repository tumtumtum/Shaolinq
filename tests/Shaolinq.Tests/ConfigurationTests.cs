// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Configuration;
using NUnit.Framework;
using Platform.Xml.Serialization;
using Shaolinq.Tests.TestModel;

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

		[Test]
		public void Test_Named_Connection_String()
		{
			var config = DataAccessModel.GetConfiguration("TestDataAccessModelNamedConnectionString");

			var model = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(config);

			Assert.That(model.GetCurrentSqlDatabaseContext().ConnectionString,
				Is.EqualTo(ConfigurationManager.ConnectionStrings["SqliteInMemory"].ConnectionString));
		}
	}
}
