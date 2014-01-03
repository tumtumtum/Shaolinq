// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using Platform;

namespace Shaolinq
{
	public class DataAccessObjectProjectionContext
		: ProjectionContext
	{
		private readonly DataAccessModel dataAccessModel;

		public DataAccessObjectProjectionContext(DataAccessModel dataAccessModel)
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
					return (T)dataAccessObjectActivator.Create();
				}
				else
				{
					return (T)this.dataAccessModel.CreateDataAccessObject(typeof(T));
				}
			}
			else
			{
				return Activator.CreateInstance<T>();
			}
		}
	}
}
