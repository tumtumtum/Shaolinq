// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence.Sql.Linq
{
	public class SqlQueryFormatterOptions
	{
		public static readonly SqlQueryFormatterOptions Default = new SqlQueryFormatterOptions();

		public bool EvaluateConstantPlaceholders { get; set; }

		public SqlQueryFormatterOptions()
		{
			this.EvaluateConstantPlaceholders = true;
		}
	}
}
