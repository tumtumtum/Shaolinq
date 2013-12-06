// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq
{
	public struct DataAccessModelDatabaseConnectionInfo
	{
		public string ConnectionName
		{
			get;
			private set;
		}
		
		public DataAccessModelDatabaseConnectionInfo(string name)
			: this()
		{
			this.ConnectionName = name;
		}
	}
}
