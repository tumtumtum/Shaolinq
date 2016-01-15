// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class RelatedDataAccessObjectsAttribute
		: NamedMemberAttribute
	{
		public string BackReferenceName { get; set; }

		public RelatedDataAccessObjectsAttribute()
		{
		}

        public RelatedDataAccessObjectsAttribute(string name, string backReferenceName)
			: base(name)
        {
	        this.BackReferenceName = backReferenceName;
        }
	}
}

