// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public abstract partial class DataAccessModelHookBase : IDataAccessModelHook
	{
		public Guid? CreateGuid()
		{
			return CreateGuid(null);
		}

		public virtual Guid? CreateGuid(PropertyDescriptor propertyDescriptor)
		{
			return null;
		}

		[RewriteAsync]
		public virtual void Create(DataAccessObject dataAccessObject)
		{
		}

		[RewriteAsync]
		public virtual void Read(DataAccessObject dataAccessObject)
		{
		}

		[RewriteAsync]
		public virtual void BeforeSubmit(DataAccessModelHookSubmitContext context)
		{
		}

		[RewriteAsync]
		public virtual void AfterSubmit(DataAccessModelHookSubmitContext context)
		{
		}

		[RewriteAsync]
		public virtual void BeforeRollback()
		{
		}

		[RewriteAsync]
		public virtual void AfterRollback()
		{
		}
	}
}