// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("SqlServer", Category = "IgnoreOnMono")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class TypeTests
		: BaseTests<TestDataAccessModel>
	{
		private readonly int floatSignificantFigures = 7;
		private readonly DateTime MinDatetime = DateTime.MinValue;
		private readonly DateTime MaxDateTime = DateTime.MaxValue;
		private readonly TimeSpan timespanEpsilon = TimeSpan.FromSeconds(1);
		
		private static TimeSpan Abs(TimeSpan timeSpan)
		{
			if (timeSpan.TotalMilliseconds < 0)
			{
				return TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds * -1);
			}

			return timeSpan;
		}

		public void Test(out bool test)
		{
			test = false;

			var x = test;
		}

		public TypeTests(string providerName)
			: base(providerName)
		{
			if (providerName == "MySql")
			{
				this.floatSignificantFigures = 6;

				this.MaxDateTime -= TimeSpan.FromDays(1);
			}
			else if (useMonoData && this.ProviderName.StartsWith("Sqlite"))
			{
				this.floatSignificantFigures = 3;
			}
		}

		[Test]
		public void Test_Min_Values()
		{
			var minDecimal = decimal.MinValue;

			//decimal.MinValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				minDecimal = long.MinValue;
			}

			this.ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MinValue,
				int.MinValue,
				long.MinValue,
				ushort.MinValue,
				uint.MinValue,
				ulong.MinValue,
				minDecimal,
				(float) TruncateToSignificantDigits(float.MinValue, this.floatSignificantFigures), // .NET internally stores 9 significant figures, but only 7 are used externally
				double.MinValue,
				false,
				Truncate(this.MinDatetime, TimeSpan.FromMilliseconds(1)),
				TimeSpan.MinValue ,
				Sex.Male,
				null,
				Encoding.UTF8.GetBytes("Hi Kitty Kat 😻😻😻"));
		}

		[Test]
		public void Test_Max_Values()
		{
			var maxDecimal = decimal.MaxValue;
		   
			//decimal.MaxValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				maxDecimal = long.MaxValue;
			}

			this.ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MaxValue,
				int.MaxValue,
				long.MaxValue,
				(ushort) short.MaxValue, // using signed max value as unsigned not supported in various databases
				(uint) int.MaxValue, // using signed max value as unsigned not supported in various databases
				(ulong) long.MaxValue, // using signed max value as unsigned not supported in various databases
				maxDecimal,
				(float) TruncateToSignificantDigits(float.MaxValue, this.floatSignificantFigures), // .NET internally stores 9 significant figures, but only 7 are used externally
				double.MaxValue,
				true,
				Truncate(this.MaxDateTime, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromDays(1) + TimeSpan.FromSeconds(1),
				Sex.Female,
				Truncate(this.MaxDateTime, TimeSpan.FromMilliseconds(1)),
				Encoding.UTF8.GetBytes("Hi Kitty Kat 😻😻😻"));
		}

		[Test]
		public void Test_Non_Integer_Values()
		{
			this.ExecuteTest(
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
				TruncateToSignificantDigits(987.654321, this.floatSignificantFigures),
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromHours(24),
				Sex.Female,
				null,
				Encoding.UTF8.GetBytes("Hi Kitty Kat 😻😻😻"));
		}

		[Test]
		public void Test_Small_Values()
		{
			var decimalValue = 0.00000000000000000001m;

			if (this.ProviderName.StartsWith("SqlServer"))
			{
				decimalValue = 0.000000001m;
			}

			this.ExecuteTest(
				"test",
				Guid.Empty,
				1,
				1,
				1,
				1,
				1,
				1,
				decimalValue,
				float.Epsilon,
				double.Epsilon,
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromMilliseconds(1),
				Sex.Female,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				Encoding.UTF8.GetBytes("\0x0\0x0\0x1"));
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
			Sex @enum,
			DateTime? nullableDateTime,
			byte[] byteArray
		)
		{
			long dbId;

			using (var scope = new TransactionScope())
			{
				var subject = this.model.ObjectWithManyTypes.Create();

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
				subject.NullableDateTime = nullableDateTime;
				subject.ByteArray = byteArray;

				scope.Flush();

				dbId = subject.Id;

				scope.Complete();
			}

			if (useMonoData && this.ProviderName.StartsWith("Sqlite"))
			{
				if (@float == TruncateToSignificantDigits(float.MaxValue, this.floatSignificantFigures))
				{
					@float = float.PositiveInfinity;
				}
				else if (@float == TruncateToSignificantDigits(float.MinValue, this.floatSignificantFigures))
				{
					@float = float.NegativeInfinity;
				}
				else if (@float == TruncateToSignificantDigits(float.Epsilon, this.floatSignificantFigures))
				{
					@float = 0;
				}

				if (@double == double.MaxValue)
				{
					@double = double.PositiveInfinity;
				}
				else if (@double == double.MinValue)
				{
					@double = double.NegativeInfinity;
				}
				else if (@double == double.Epsilon)
				{
					@double = 0;
				}
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = this.model.ObjectWithManyTypes.Single(x => x.Id == dbId);

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
				this.AssertDateTime(dbObj.DateTime, dateTime);
				Assert.That(Abs(dbObj.TimeSpan - timeSpan), Is.LessThan(this.timespanEpsilon));
				Assert.That(dbObj.Enum, Is.EqualTo(@enum));
				AssertNullable(dbObj.NullableDateTime, nullableDateTime, this.AssertDateTime);
				Assert.That(dbObj.ByteArray, Is.EqualTo(byteArray));
			}
		}

		private static void AssertNullable<T>(T? nullable1, T? nullable2, Action<T, T> assertUnderlying) where T : struct
		{
			Assert.That(nullable1.HasValue, Is.EqualTo(nullable2.HasValue));

			if (nullable1.HasValue && nullable2.HasValue)
			{
				assertUnderlying(nullable1.Value, nullable2.Value);
			}
			else
			{
				Assert.That(nullable1, Is.Null);
				Assert.That(nullable2, Is.Null);
			}
		}

		private void AssertDateTime(DateTime dateTime1, DateTime dateTime2)
		{
			Assert.That(Abs(dateTime1.ToUniversalTime() - dateTime2.ToUniversalTime()), Is.LessThan(this.timespanEpsilon));
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