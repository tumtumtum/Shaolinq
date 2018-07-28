// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessTypeAttribute
		: Attribute
	{
		public string Name { get; set; }

		public DataAccessTypeAttribute()
		{	
		}

		public DataAccessTypeAttribute(string name)
		{
			this.Name = name;
		}

		internal string GetName(TypeDescriptor type, string transformString = "")
		{
			return VariableSubstituter.SedTransform(VariableSubstituter.Substitute(this.Name ?? type.TypeName, type), transformString);
		}
	}
}
