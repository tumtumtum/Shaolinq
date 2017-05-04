// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence
{
	public struct TypedValue
	{
		public Type Type {get; }
		public object Value { get; }
		public Func<object, object> Converter { get; }

		public TypedValue(Type type, object value, Func<object, object> converter = null)
			: this()
		{
			this.Type = type;
			this.Value = converter == null ? value : converter(value);
			this.Converter = converter ?? (c => c);
		}

		public TypedValue ChangeValue(object value)
		{
			value = this.Converter(value);

			if (value != null && !this.Type.IsInstanceOfType(value))
			{
				throw new InvalidOperationException($"{nameof(value)} is not of type {this.Type.Name}");
			}

			return new TypedValue(this.Type, value, this.Converter);
		}
	}
}