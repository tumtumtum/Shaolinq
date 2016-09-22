using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
    public class BaseClass<T>
    {
        [RewriteAsync]
        public virtual void Foo<D>(T parameter, D domainObject)
        {
        }
    }

    public class DerivedClass : BaseClass<Guid>
    {
        [RewriteAsync]
        public override void Foo<E>(Guid parameter, E domainObject)
        {   
        }
    }
}
