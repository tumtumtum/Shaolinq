using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlObjectReference
		: SqlBaseExpression
	{
		public ReadOnlyCollection<MemberAssignment> Bindings { get; private set; }

		public SqlObjectReference(Type type, MemberAssignment[] bindings)
			: this(type, (IEnumerable<MemberAssignment>)bindings)
		{	
		}

		public SqlObjectReference(Type type, IEnumerable<MemberAssignment> bindings)
			: base(type)
		{
			this.Bindings = new ReadOnlyCollection<MemberAssignment>(bindings.ToList());
		}
	}
}
