// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.TypeBuilding
{
	public interface IDataAccessObjectInternal
	{
		void SetIsNew(bool value);
		void SetIsDeleted(bool value);
		object CompositePrimaryKey { get; }
		IDataAccessObjectInternal NotifyRead();
		void MarkServerSidePropertiesAsApplied();
		IDataAccessObjectInternal SubmitToCache();
		IDataAccessObjectInternal ResetModified();
		int GetHashCodeAccountForServerGenerated();
		LambdaExpression DeflatedPredicate { get; }
		IDataAccessObjectInternal FinishedInitializing();
		IDataAccessObjectInternal SetDeflatedPredicate(LambdaExpression value);
		IDataAccessObjectInternal SetPrimaryKeys(ObjectPropertyValue[] primaryKeys);
		bool HasAnyChangedPrimaryKeyServerSideProperties { get; }
		IDataAccessObjectInternal SetIsDeflatedReference(bool value);
		bool EqualsAccountForServerGenerated(object dataAccessObject);
		bool ComputeServerGeneratedIdDependentComputedTextProperties();
		bool ComputeNonServerGeneratedIdDependentComputedTextProperties();
		ObjectPropertyValue[] GetPrimaryKeysFlattened(out bool predicated);
		void SwapData(DataAccessObject source, bool transferChangedProperties);
		ObjectPropertyValue[] GetPrimaryKeysForUpdateFlattened(out bool predicated);
		List<ObjectPropertyValue> GetChangedPropertiesFlattened(out bool predicated);
		bool ValidateServerSideGeneratedIds();
	}
}	