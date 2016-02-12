// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
		public static IDataReader ExecuteReaderEx(this IDbCommand command)
		{
			var marsDbCommand = command as MarsDbCommand;

			if (marsDbCommand != null)
			{
				return marsDbCommand.ExecuteReader();
			}

			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return dbCommand.ExecuteReader();
			}
			
			return command.ExecuteReader();
		}
		
		[RewriteAsync]
		public static int ExecuteNonQueryEx(this IDbCommand command)
		{
			var marsDbCommand = command as MarsDbCommand;

			if (marsDbCommand != null)
			{
				return marsDbCommand.ExecuteNonQuery();
			}

			var dbCommand = command as DbCommand;

			if (dbCommand != null)
			{
				return dbCommand.ExecuteNonQuery();
			}

			return command.ExecuteNonQuery();
		}
	}
}
