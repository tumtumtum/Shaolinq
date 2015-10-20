// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public class ObjectAlreadyInitializedException
		: Exception
	{
		public Object RelatedObject { get; }

		public ObjectAlreadyInitializedException(object relatedObject)
		{
			this.RelatedObject = relatedObject;
		}
	}
}
