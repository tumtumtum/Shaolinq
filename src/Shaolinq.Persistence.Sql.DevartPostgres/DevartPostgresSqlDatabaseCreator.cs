// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Postgres.Devart
{
	public class DevartPostgresSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public DevartPostgresSqlDatabaseCreator(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
		}
	}
}
