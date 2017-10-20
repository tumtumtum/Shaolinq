// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	partial class QueryDerived<T> : Query<T>
	{
		[RewriteAsync]
		public void Test()
		{
			var accountQuery = default(IAccountQuery);

			accountQuery.GetAll().Where(c => true);
		}

		[RewriteAsync]
		public override T GetById<I>(I id)
		{
			return default(T);
		}

		[RewriteAsync]
		public override T GetById2<I>(I id)
		{
			throw new NotImplementedException();
		}

		[RewriteAsync]
		public override T GetById3<I>(I id)
		{
			throw new NotImplementedException();
		}

		[RewriteAsync]
		public List<Account> GetAllAccounts(bool deleted = false)
		{
			return null;
		}
	}

	public partial interface IQuery<T>
	{
		[RewriteAsync]
		T GetById<I>(I id) where I : class;
		T GetById2<I>(I id) where I : List<I>;
		T GetById3<I>(I id) where I : new();

		[RewriteAsync]
		IQueryable<T> GetAll(bool deleted = false);
	}

	public class Account
	{
	}

	public interface IAccountQuery : IQuery<Account>
	{	
	}

	public partial class Query<T>: IQuery<T>
	{
		public virtual T GetById<I>(I id)
			where I : class
		{
			return default(T);
		}

		[RewriteAsync]
		public IQueryable<T> GetAll(bool deleted = false)
		{
			return null;
		}

		public virtual Task<T> GetByIdAsync<I>(I id)
			where I : class
		{
			return GetByIdAsync(id, CancellationToken.None);
		}

		public virtual Task<T> GetByIdAsync<I>(I id, CancellationToken ct)
			where I : class
		{
			return null;
		}

		public virtual T GetById2<I>(I id) where I : List<I>
		{
			throw new NotImplementedException();
		}
		
		public virtual T GetById3<I>(I id) where I : new()
		{
			throw new NotImplementedException();
		}
	}
}
