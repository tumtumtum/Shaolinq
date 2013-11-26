// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Reflection;
using Platform.Reflection;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class AssemblyDefaultPersistenceContextAttribute
		: Attribute
	{
		public string Name { get; set; }

		public static string GetDefaultPersistenceContextName(Assembly assembly)
		{
			var attribute = assembly.GetFirstCustomAttribute<AssemblyDefaultPersistenceContextAttribute>(false);

			return attribute != null ? attribute.Name : null;
		}

		public AssemblyDefaultPersistenceContextAttribute(string name)
		{
			this.Name = name;
		}
	}
}
