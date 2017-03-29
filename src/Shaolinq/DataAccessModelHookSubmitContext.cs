// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq
{
	public class DataAccessModelHookSubmitContext
	{
		public bool IsFlush { get; }

		public IEnumerable<DataAccessObject> New => this.dataContext.cachesByType.SelectMany(cache => cache.Value.GetNewObjects());
		public IEnumerable<DataAccessObject> Updated => this.dataContext.cachesByType.SelectMany(cache => cache.Value.GetObjectsByPredicate().Concat(cache.Value.GetObjectsById()));
		public IEnumerable<DataAccessObject> Deleted => this.dataContext.cachesByType.SelectMany(cache => cache.Value.GetDeletedObjects());

		private readonly DataAccessObjectDataContext dataContext;

		internal DataAccessModelHookSubmitContext(DataAccessObjectDataContext dataContext, bool isFlush)
		{
			this.dataContext = dataContext;
			this.IsFlush = isFlush;
		}
	}
}