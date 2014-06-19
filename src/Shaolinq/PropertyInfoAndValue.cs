// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Reflection;

namespace Shaolinq
{
	public struct PropertyInfoAndValue
	{
		public readonly object value;
		public readonly string propertyName;
		public readonly string persistedName;
		public readonly bool isSynthetic;
		public readonly int propertyNameHashCode;
		public readonly PropertyInfo propertyInfo;

		public PropertyInfoAndValue(PropertyInfo propertyInfo, object value, string propertyName, string persistedName, bool isSynthetic, int propertyNameHashCode)
		{
			this.value = value;
			this.isSynthetic = isSynthetic;
			this.propertyName = propertyName;
			this.propertyInfo = propertyInfo;
			this.persistedName = persistedName;
			this.propertyNameHashCode = propertyNameHashCode;
		}

		public override bool Equals(object obj)
		{
			var typedObj = obj as PropertyInfoAndValue?;

			if (typedObj == null)
			{
				return false;
			}

			return this == typedObj.Value;
		}

		public static bool operator==(PropertyInfoAndValue left, PropertyInfoAndValue right)
		{
			if (!Object.ReferenceEquals(left.propertyName, right.propertyName))
			{
				return false;
			}

			if (!Object.ReferenceEquals(left.persistedName, right.persistedName))
			{
				return false;
			}

			if (!Object.Equals(left.value, right.value))
			{
				return false;
			}

			if (left.propertyNameHashCode != right.propertyNameHashCode)
			{
				return false;
			}

			return true;
		}

		public static bool operator !=(PropertyInfoAndValue left, PropertyInfoAndValue right)
		{
			if (!Object.ReferenceEquals(left.propertyName, right.propertyName))
			{
				return true;
			}

			if (!Object.ReferenceEquals(left.persistedName, right.persistedName))
			{
				return true;
			}

			if (!Object.Equals(left.value, right.value))
			{
				return true;
			}

			if (left.propertyNameHashCode != right.propertyNameHashCode)
			{
				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			var hashCode = 0;

			if (this.value != null)
			{
				hashCode = this.value.GetHashCode();
			}

			return hashCode ^ this.propertyNameHashCode;
		}

		public override string ToString()
		{
			return String.Format("{0}|{1}|{2}={3}", this.propertyName, this.persistedName, this.isSynthetic ? "synthetic" : "", this.value);
		}
	}
}
