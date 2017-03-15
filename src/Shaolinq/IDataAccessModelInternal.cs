// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;

namespace Shaolinq
{
	public interface IDataAccessModelInternal
	{
		IQueryable CreateDataAccessObjects(Type type);
		IQueryable CreateDataAccessObjects(RuntimeTypeHandle typeHandle);
	}
}