// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence
{
	public static class DataRecordMethods
	{
		public static readonly MethodInfo GetBooleanMethod = TypeUtils.GetMethod<IDataRecord>(c => c.GetBoolean(default(int)));
		public static readonly MethodInfo GetInt32Method = TypeUtils.GetMethod<IDataRecord>(c => c.GetInt32(default(int)));
		public static readonly MethodInfo GetInt64Method = TypeUtils.GetMethod<IDataRecord>(c => c.GetInt64(default(int)));
		public static readonly MethodInfo GetStringMethod = TypeUtils.GetMethod<IDataRecord>(c => c.GetString(default(int)));
		public static readonly MethodInfo GetValueMethod = TypeUtils.GetMethod<IDataRecord>(c => c.GetValue(default(int)));
		public static readonly MethodInfo GetGuidMethod = TypeUtils.GetMethod<IDataRecord>(c => c.GetGuid(default(int)));
		public static readonly MethodInfo IsNullMethod = TypeUtils.GetMethod<IDataRecord>(c => c.IsDBNull(default(int)));
		
		public static MethodInfo GetMethod(string name)
		{
			switch (name)
			{
			case "GetBoolean":
				return GetBooleanMethod;
			case "GetInt32":
				return GetInt32Method;
			case "GetInt64":
				return GetInt64Method;
			case "GetString":
				return GetStringMethod;
			case "GetValue":
				return GetValueMethod;
			case "GetGuid":
				return GetGuidMethod;
			default:
				var retval = typeof(IDataRecord).GetMethod(name);

				if (retval == null)
				{
					throw new ArgumentException($"Invalid method name {name}", nameof(name));
				}

				return retval;
			}
		}
	}
}
