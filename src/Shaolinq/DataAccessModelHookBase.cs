using System;
using System.Threading;
using System.Threading.Tasks;
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
	}
}