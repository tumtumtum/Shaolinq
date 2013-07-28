using System;
using System.Collections.Generic;
using Platform;

namespace Shaolinq
{
	internal class RelatedDataAccessObjectsInitializeActionsCache
	{
		internal readonly Dictionary<Pair<Type, Type>, Action<IDataAccessObject, IDataAccessObject>> initializeActions = new Dictionary<Pair<Type, Type>, Action<IDataAccessObject, IDataAccessObject>>();
	}
}
