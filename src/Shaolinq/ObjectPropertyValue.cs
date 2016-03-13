// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public struct ObjectPropertyValue
	{
		public object Value { get; }
		public Type PropertyType { get; }
		public string PropertyName { get; }
		public string PersistedName { get; }
		public int PropertyNameHashCode { get; }

		internal static ObjectPropertyValue Create(PropertyDescriptor property, object target)
		{
			var value = property.PropertyInfo.GetValue(target);

			return new ObjectPropertyValue(property.PropertyType, property.PropertyName, property.PersistedName, property.PropertyName.GetHashCode(), value);
		}
		
		public ObjectPropertyValue(Type propertyType, string propertyName, string persistedName, int propertyNameHashcode, object value)
			: this()
		{
			this.Value = value;
			this.PropertyName = propertyName;
			this.PersistedName = persistedName;
			this.PropertyNameHashCode = propertyNameHashcode;
			this.PropertyType = propertyType;
		}

		public override bool Equals(object obj)
		{
			var typedObj = obj as ObjectPropertyValue?;

			if (typedObj == null)
			{
				return false;
			}

			return this == typedObj.Value;
		}

		public static bool operator==(ObjectPropertyValue left, ObjectPropertyValue right)
		{
			if (!ReferenceEquals(left.PropertyName, right.PropertyName))
			{
				return false;
			}

			if (!ReferenceEquals(left.PersistedName, right.PersistedName))
			{
				return false;
			}

			if (left.PropertyNameHashCode != right.PropertyNameHashCode)
			{
				return false;
			}

			if (!Object.Equals(left.Value, right.Value))
			{
				return false;
			}

			return true;
		}

		public static bool operator !=(ObjectPropertyValue left, ObjectPropertyValue right)
		{
			if (left.PropertyNameHashCode != right.PropertyNameHashCode)
			{
				return true;
			}

			if (!ReferenceEquals(left.PropertyName, right.PropertyName))
			{
				return true;
			}

			if (!ReferenceEquals(left.PersistedName, right.PersistedName))
			{
				return true;
			}

			if (!Object.Equals(left.Value, right.Value))
			{
				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			var hashCode = 0;

			if (this.Value != null)
			{
				hashCode = this.Value.GetHashCode();
			}

			return hashCode ^ this.PropertyNameHashCode;
		}

		public override string ToString()
		{
			return $"{this.PropertyName}|{this.PersistedName}|={this.Value}";
		}
	}
}
