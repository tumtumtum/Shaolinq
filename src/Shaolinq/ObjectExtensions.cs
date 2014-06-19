// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using Platform;

namespace Shaolinq
{
	internal static class ObjectExtensions
	{
		public static void PopulateFrom<T, U>(this T value, U source)
		{
			ProjectionContext projectionContext;
			var valueDataAccessObject = value as IDataAccessObject;
			
			if (valueDataAccessObject != null)
			{
				projectionContext = valueDataAccessObject.DataAccessModel.DataAccessObjectProjectionContext;
			}
			else
			{
				projectionContext = ProjectionContext.Default;
			}

			ProjectionContext.ProjectInto(projectionContext, value, source);
		}
	}
}
