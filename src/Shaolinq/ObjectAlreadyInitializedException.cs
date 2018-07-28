// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public class ObjectAlreadyInitializedException
		: Exception
	{
		public object RelatedObject { get; }

		public ObjectAlreadyInitializedException(object relatedObject)
		{
			this.RelatedObject = relatedObject;
		}
	}
}
