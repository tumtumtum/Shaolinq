// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class DataAccessTransactionAbortedException
		: Exception
	{
		public DataAccessTransactionAbortedException()
		{
		}

		public DataAccessTransactionAbortedException(string exception)
			: base(exception)
		{
		}


		public DataAccessTransactionAbortedException(Exception innerException)
			: base("TransactionAborted", innerException)
		{
		}

		public DataAccessTransactionAbortedException(string message, Exception innerException)
			: base(message, innerException)
		{	
		}
	}
}