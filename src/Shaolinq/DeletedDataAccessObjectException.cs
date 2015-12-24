// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq
{
	public class DeletedDataAccessObjectException
		: MissingDataAccessObjectException
	{
		public DeletedDataAccessObjectException(IDataAccessObjectAdvanced dataAccessObject)
			: base(dataAccessObject)
		{	
		}
	}
}
