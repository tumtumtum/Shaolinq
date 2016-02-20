// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence
{
	public struct TypedValue
	{
		public Type Type {get; private set;}
		public object Value { get; private set; }

		public TypedValue(Type type, object value)
			: this()
		{
			this.Type = type;
			this.Value = value;
		}
	}
}