// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
			var s = VariableSubstituter.SedTransform("Pokwer", "s/o/0/g");

			Assert.AreEqual("P0kwer", s);

			s = VariableSubstituter.SedTransform("Pokwer", "s/([A-Z]).*/$1$0/g");

			Assert.AreEqual("PPokwer", s);

			s = VariableSubstituter.SedTransform("DbPokwer", "s/Db(Pokwer)/$1/g");

			Assert.AreEqual("Pokwer", s);

			s = VariableSubstituter.SedTransform("Pokwer22", "s/Db(Pokwer)/$1/g");

			Assert.AreEqual("Pokwer22", s);
		}
	}
}
