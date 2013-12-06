// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using Shaolinq.Persistence;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static DatabaseConnection GetDatabaseConnection(this IDataAccessObject dataAccessObject)
		{
			return dataAccessObject.DataAccessModel.GetDatabaseConnection(dataAccessObject);
		}
	}
}
