// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Data;
using System.Reflection;

namespace Shaolinq.Persistence
{
	public static class DataRecordMethods
	{
		public static readonly MethodInfo GetBooleanMethod = typeof(IDataRecord).GetMethod("GetBoolean");
		public static readonly MethodInfo GetInt32Method = typeof(IDataRecord).GetMethod("GetInt32");
		public static readonly MethodInfo GetInt64Method = typeof(IDataRecord).GetMethod("GetInt64");
		public static readonly MethodInfo GetStringMethod = typeof(IDataRecord).GetMethod("GetString");
		public static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod("GetValue");
		public static readonly MethodInfo GetGuidMethod = typeof(IDataRecord).GetMethod("GetGuid");
		public static readonly MethodInfo IsNullMethod = typeof(IDataRecord).GetMethod("IsDBNull");

		public static MethodInfo GetMethod(string name)
		{
			switch (name)
			{
				case "GetBoolean":
					return GetBooleanMethod;
				case "GetString":
					return GetStringMethod;
				case "GetInt32":
					return GetInt32Method;
				case "GetInt64":
					return GetInt64Method;
				case "IsNull":
					return IsNullMethod;
				case "GetGuid":
					return GetGuidMethod;
				default:
					return typeof(IDataRecord).GetMethod(name);
			}
		}
	}
}
