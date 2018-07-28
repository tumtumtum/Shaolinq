// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq
{
	public interface IDataAccessObjectActivator
	{
		IDataAccessObjectAdvanced Create();
		IDataAccessObjectAdvanced Create<K>(K primaryKey);
	}

	public interface IDataAccessObjectActivator<out T>
		: IDataAccessObjectActivator
		where T : DataAccessObject
	{
		new T Create();
		new T Create<K>(K primaryKey);
	}
}
