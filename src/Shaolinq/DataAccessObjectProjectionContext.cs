// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using Platform;

namespace Shaolinq
{
	public class DataAccessObjectProjectionContext
		: ProjectionContext
	{
		private readonly BaseDataAccessModel dataAccessModel;

		public DataAccessObjectProjectionContext(BaseDataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;	
		}

		public override T CreateInstance<T>(object context)
		{
			if (typeof(T).IsDataAccessObjectType())
			{
				var dataAccessObjectActivator = context as IDataAccessObjectActivator;

				if (dataAccessObjectActivator != null)
				{
					return (T)dataAccessObjectActivator.NewDataAccessObject();
				}
				else
				{
					return (T)this.dataAccessModel.NewDataAccessObject(typeof(T));
				}
			}
			else
			{
				return Activator.CreateInstance<T>();
			}
		}
	}
}
