// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 namespace Shaolinq
{
	public interface IDataAccessObjectActivator
	{
		IDataAccessObject Create();
		IDataAccessObject Create<K>(K primaryKey);
	}

	public interface IDataAccessObjectActivator<out T>
		: IDataAccessObjectActivator
		where T : IDataAccessObject
	{
		new T Create();
		new T Create<K>(K primaryKey);
	}
}
