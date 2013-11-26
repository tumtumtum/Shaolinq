// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class TestConstraints
		: BaseTests
	{
		public TestConstraints(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Size_Constraint()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();
				var student = school.Students.NewDataAccessObject();

				student.Email = new string('A', 63);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();
				var student = school.Students.NewDataAccessObject();

				student.Email = new string('A', 64);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();
				var student = school.Students.NewDataAccessObject();

				student.Email = new string('A', 65);

				scope.Complete();
			}
		}
	}
}
