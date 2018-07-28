// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence
{
	public class ExecuteReaderContext : IDisposable
	{
		public IDbCommand DbCommand { get; private set; }
		public IDataReader DataReader { get; private set; }
		
		public ExecuteReaderContext(IDataReader dataReader, IDbCommand dbCommand)
		{
			this.DataReader = dataReader;
			this.DbCommand = dbCommand;
		}

		public void Dispose()
		{
			this.DataReader?.Dispose();
			this.DbCommand?.Dispose();

			this.DataReader = null;
			this.DbCommand = null;
		}
	}
}