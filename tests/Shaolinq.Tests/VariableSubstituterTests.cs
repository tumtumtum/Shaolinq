// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using NUnit.Framework;
using Shaolinq.Persistence;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class VariableSubstituterTests
	{
		[Test]
		public void Test()
		{
			var s = VariableSubstituter.SedTransform("", "s/^$/HELLO/g s/HELL(.*)/BYE$1/");

			Assert.AreEqual("BYEO", s);

			s = VariableSubstituter.SedTransform("", "s/^$/HELLO/g \n\ts/HELL(.*)/BYE$1/");

			Assert.AreEqual("BYEO", s);

			s = VariableSubstituter.SedTransform("", "s/^$/HELLO/g \n\ts/HELL(.*)/BYE$1/     ");

			Assert.AreEqual("BYEO", s);

			s = VariableSubstituter.SedTransform("Pokwer22", "s/^.*$/$(env.USERNAME)$0/g");

			Assert.AreEqual(Environment.GetEnvironmentVariable("USERNAME") + "Pokwer22", s);

			s = VariableSubstituter.SedTransform("Pokwer", "s/o/0/g");

			Assert.AreEqual("P0kwer", s);

			s = VariableSubstituter.SedTransform("Pokwer", "s/([A-Z]).*/$1$0/g");

			Assert.AreEqual("PPokwer", s);

			s = VariableSubstituter.SedTransform("DbPokwer", "s/Db(Pokwer)/$1/g");

			Assert.AreEqual("Pokwer", s);

			s = VariableSubstituter.SedTransform("Pokwer22", "s/Db(Pokwer)/$1/g");

			Assert.AreEqual("Pokwer22", s);

			s = VariableSubstituter.SedTransform("Hello", "s/^$/HELLO/g");

			Assert.AreEqual("Hello", s);

			s = VariableSubstituter.SedTransform("", "s/^$/HELLO/g");

			Assert.AreEqual("HELLO", s);
		}
	}
}
