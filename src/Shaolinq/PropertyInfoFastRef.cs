// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Reflection;

namespace Shaolinq
{
	public static class PropertyInfoFastRef
	{
		public static readonly PropertyInfo DataAccessObjectObjectState = typeof(IDataAccessObjectAdvanced).GetProperty("ObjectState");
		public static readonly PropertyInfo DataAccessObjectInternalIsNewProperty = typeof(IDataAccessObjectAdvanced).GetProperty("IsNew", BindingFlags.Public | BindingFlags.Instance);
		public static readonly PropertyInfo DataAccessObjectInternaReferencesNewUncommitedRelatedObject = typeof(IDataAccessObjectAdvanced).GetProperty("ReferencesNewUncommitedRelatedObject", BindingFlags.Public | BindingFlags.Instance);
		public static readonly PropertyInfo DataAccessObjectInternaIsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys = typeof(IDataAccessObjectAdvanced).GetProperty("IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys", BindingFlags.Public | BindingFlags.Instance);
		public static readonly PropertyInfo ObjectPropertyValueValueProperty = typeof(ObjectPropertyValue).GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
	}
}
