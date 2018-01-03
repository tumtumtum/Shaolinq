// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public static partial class DbCommandExtensions
	{
		public static T Unwrap<T>(this IDbCommand command)
			where T : class, IDbCommand => command as T ?? (T)(command as MarsDbCommand)?.Inner;

		[RewriteAsync]
		public static IDataReader ExecuteReaderEx(this IDbCommand command, DataAccessModel dataAccessModel, bool suppressAnalytics = false)
		{
			if (command is MarsDbCommand marsDbCommand)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return marsDbCommand.ExecuteReader();
			}

			if (command is DbCommand dbCommand)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return dbCommand.ExecuteReader();
			}
			
			return command.ExecuteReader();
		}
		
		[RewriteAsync]
		public static int ExecuteNonQueryEx(this IDbCommand command, DataAccessModel dataAccessModel, bool suppressAnalytics = false)
		{
			if (command is MarsDbCommand marsDbCommand)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return marsDbCommand.ExecuteNonQuery();
			}


			if (command is DbCommand dbCommand)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return dbCommand.ExecuteNonQuery();
			}

			return command.ExecuteNonQuery();
		}
	}
}
