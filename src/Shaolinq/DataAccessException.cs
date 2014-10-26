// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public class DataAccessException
		: Exception
	{
		public string RelatedQuery { get; set; }

		public DataAccessException()
		{
		}

		public DataAccessException(string message, string relatedQuery)
			: base(message)
		{
			this.RelatedQuery = relatedQuery;
		}

		public DataAccessException(Exception innerException, string relatedQuery)
			: base(innerException != null ? innerException.Message : "DataAccessException", innerException)
		{
			this.RelatedQuery = relatedQuery;
		}
	}
}
