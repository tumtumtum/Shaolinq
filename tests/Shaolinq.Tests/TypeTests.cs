using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	/*[TestFixture("MySql")]*/
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class TypeTests
		: BaseTests
	{
		public TypeTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Min_Values()
		{
			decimal minDecimal = decimal.MinValue;

			//decimal.MinValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				minDecimal = long.MinValue;
			}

			ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MinValue,
				int.MinValue,
				long.MinValue,
				ushort.MinValue,
				uint.MinValue,
				ulong.MinValue,
				minDecimal,
				(float) TruncateToSignificantDigits(float.MinValue, 7), // .NET internally stores 9 significant figures, but only 7 are used externally
				double.MinValue,
				false,
				Truncate(DateTime.MinValue, TimeSpan.FromMilliseconds(1)),
				TimeSpan.MinValue,
				Sex.Male);
		}

		[Test]
		public void Test_Max_Values()
		{
			decimal maxDecimal = decimal.MaxValue;

			//decimal.MaxValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				maxDecimal = long.MaxValue;
			}

			ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MaxValue,
				int.MaxValue,
				long.MaxValue,
				(ushort) short.MaxValue, // using signed max value as unsigned not supported in various databases
				(uint) int.MaxValue, // using signed max value as unsigned not supported in various databases
				(ulong) long.MaxValue, // using signed max value as unsigned not supported in various databases
				maxDecimal,
				(float) TruncateToSignificantDigits(float.MaxValue, 7), // .NET internally stores 9 significant figures, but only 7 are used externally
				double.MaxValue,
				true,
				Truncate(DateTime.MaxValue, TimeSpan.FromMilliseconds(1)),
				TimeSpan.MaxValue,
				Sex.Female);
		}

		[Test]
		public void Test_Non_Integer_Values()
		{
			ExecuteTest(
				"test",
				Guid.Empty,
				987,
				987654,
				987654321,
				123,
				123456,
				123456789,
				123.456789m,
				123.456f,
				987.654321,
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromHours(24),
				Sex.Female);
		}

		[Test]
		public void Test_Small_Values()
		{
			ExecuteTest(
				"test",
				Guid.Empty,
				1,
				1,
				1,
				1,
				1,
				1,
				0.00000000000000000001m,
				float.Epsilon,
				double.Epsilon,
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromMilliseconds(1),
				Sex.Female);
		}

		private void ExecuteTest(
			string @string,
			Guid guid,
			short @short,
			int @int,
			long @long,
			ushort @ushort,
			uint @uint,
			ulong @ulong,
			decimal @decimal,
			float @float,
			double @double,
			bool @bool,
			DateTime dateTime,
			TimeSpan timeSpan,
			Sex @enum
		)
		{
			long dbId;

			using (var scope = new TransactionScope())
			{
				var subject = model.ObjectWithManyTypes.Create();

				subject.String = @string;
				subject.Guid = guid;
				subject.Short = @short;
				subject.Int = @int;
				subject.Long = @long;
				subject.UShort = @ushort;
				subject.UInt = @uint;
				subject.ULong = @ulong;
				subject.Decimal = @decimal;
				subject.Float = @float;
				subject.Double = @double;
				subject.Bool = @bool;
				subject.DateTime = dateTime;
				subject.TimeSpan = timeSpan;
				subject.Enum = @enum;
				//subject.ByteArray = byteArray;

				scope.Flush(model);

				dbId = subject.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = model.ObjectWithManyTypes.Single(x => x.Id == dbId);

				Assert.That(dbObj.String, Is.EqualTo(@string));
				Assert.That(dbObj.Guid, Is.EqualTo(guid));
				Assert.That(dbObj.Short, Is.EqualTo(@short));
				Assert.That(dbObj.Int, Is.EqualTo(@int));
				Assert.That(dbObj.Long, Is.EqualTo(@long));
				Assert.That(dbObj.UShort, Is.EqualTo(@ushort));
				Assert.That(dbObj.UInt, Is.EqualTo(@uint));
				Assert.That(dbObj.ULong, Is.EqualTo(@ulong));
				Assert.That(dbObj.Decimal, Is.EqualTo(@decimal));
				Assert.That(dbObj.Float, Is.EqualTo(@float));
				Assert.That(dbObj.Double, Is.EqualTo(@double));
				Assert.That(dbObj.Bool, Is.EqualTo(@bool));
				Assert.That(dbObj.DateTime.ToUniversalTime(), Is.EqualTo(dateTime.ToUniversalTime()));
				Assert.That(dbObj.TimeSpan, Is.EqualTo(timeSpan));
				Assert.That(dbObj.Enum, Is.EqualTo(@enum));
				//Assert.That(dbObj.ByteArray, Is.EqualTo(byteArray));
			}
		}

		private static DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
		{
			return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
		}

		private static double TruncateToSignificantDigits(double d, int digits)
		{
			if (d == 0)
				return 0;

			var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
			return scale * Math.Truncate(d / scale);
		}
	}
}