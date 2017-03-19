// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public static partial class DbCommandExtensions
	{
		public static T Unwrap<T>(this IDbCommand command)
			where T : class, IDbCommand
		{
			return (command as T) ?? (T)(command as MarsDbCommand)?.Inner;
		}

		[RewriteAsync]
		public static IDataReader ExecuteReaderEx(this IDbCommand command, DataAccessModel dataAccessModel, bool suppressAnalytics = false)
		{
			var marsDbCommand = command as MarsDbCommand;

			if (marsDbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return marsDbCommand.ExecuteReader();
			}

			var dbCommand = command as DbCommand;

			if (dbCommand != null)
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
			var marsDbCommand = command as MarsDbCommand;

			if (marsDbCommand != null)
			{
				if (!suppressAnalytics)
				{
					dataAccessModel.queryAnalytics.IncrementQueryCount();
				}

				return marsDbCommand.ExecuteNonQuery();
			}

			var dbCommand = command as DbCommand;

			if (dbCommand != null)
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
