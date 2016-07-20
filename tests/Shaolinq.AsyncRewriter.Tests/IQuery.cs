using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	partial interface IQuery<out T>
	{
		T GetById<I>(I id) where I : class;
		T GetById2<I>(I id) where I : List<I>;
		T GetById3<I>(I id) where I : new();
	}

	partial class Query<T>: IQuery<T>
	{
		public virtual T GetById<I>(I id)
			where I : class
		{
			return default(T);
		}
		
		public virtual Task<T> GetByIdAsync<I>(I id)
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

	partial class QueryDerived<T> : Query<T>
	{
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
	}
}
