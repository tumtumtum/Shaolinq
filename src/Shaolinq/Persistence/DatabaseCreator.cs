// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq.Persistence
{
	public abstract class DatabaseCreator
	{
		public abstract bool CreateDatabase(bool overwrite);
	}
}
