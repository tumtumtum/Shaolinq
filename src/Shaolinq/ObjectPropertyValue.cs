// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public struct ObjectPropertyValue
	{
		public object Value { get; private set; }
		public Type PropertyType { get; private set; }
		public string PropertyName { get; private set; }
		public string PersistedName { get; private set; }
		public int PropertyNameHashCode { get; private set; }
		
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
			if (!Object.ReferenceEquals(left.PropertyName, right.PropertyName))
			{
				return false;
			}

			if (!Object.ReferenceEquals(left.PersistedName, right.PersistedName))
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

			if (!Object.ReferenceEquals(left.PropertyName, right.PropertyName))
			{
				return true;
			}

			if (!Object.ReferenceEquals(left.PersistedName, right.PersistedName))
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
			return String.Format("{0}|{1}|={2}", this.PropertyName, this.PersistedName, this.Value);
		}
	}
}
