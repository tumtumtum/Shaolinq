// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class RelatedDataAccessObjectsAttribute
		: NamedMemberAttribute
	{
		public RelatedDataAccessObjectsAttribute()
		{
		}

        public RelatedDataAccessObjectsAttribute(string name)
			: base(name)
		{
		}
	}
}

