// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence
{
	public class EnumTypeDescriptor
	{
		public string Name { get; }
		public Type EnumType { get; }
		
		public EnumTypeDescriptor(Type enumType)
		{
			this.EnumType = enumType;
			this.Name = enumType.Name;
		}

		public string[] GetValues()
		{
			return Enum.GetNames(this.EnumType);
		}
	}
}
