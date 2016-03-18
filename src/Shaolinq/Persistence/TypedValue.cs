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

		public TypedValue ChangeValue(object value)
		{
			if (value != null && !this.Type.IsInstanceOfType(value))
			{
				throw new InvalidOperationException($"{nameof(value)} is not of type {this.Type.Name}");
			}

			return new TypedValue(this.Type, value);
		}
	}
}