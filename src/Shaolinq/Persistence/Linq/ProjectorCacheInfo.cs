// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq
{
	internal struct ProjectorCacheInfo
	{
		public Delegate projector;
	    public Delegate asyncProjector;
	}
}