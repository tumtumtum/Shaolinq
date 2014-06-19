// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using Platform;

namespace Shaolinq
{
	internal class RelatedDataAccessObjectsInitializeActionsCache
	{
		internal readonly Dictionary<Pair<Type, Type>, Action<IDataAccessObject, IDataAccessObject>> initializeActions = new Dictionary<Pair<Type, Type>, Action<IDataAccessObject, IDataAccessObject>>();
	}
}
