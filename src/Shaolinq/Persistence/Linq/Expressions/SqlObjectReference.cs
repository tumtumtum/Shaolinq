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
		public ReadOnlyCollection<MemberBinding> Bindings { get; private set; }

		public IEnumerable<MemberBinding> GetBindingsFlattened()
		{
			foreach (var binding in this.Bindings)
			{
				var assignment = binding as MemberAssignment;

				if (assignment == null)
				{
					yield return binding;

					continue;
				}

				if (assignment.Expression is SqlObjectReference)
				{
					foreach (var value in ((SqlObjectReference)assignment.Expression).GetBindingsFlattened())
					{
						yield return value;
					}
				}
				else
				{
					yield return assignment;
				}
			}
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ObjectReference;
			}
		}

		public SqlObjectReference(Type type, MemberBinding[] bindings)
			: this(type, (IEnumerable<MemberAssignment>)bindings)
		{	
		}

		public SqlObjectReference(Type type, IEnumerable<MemberBinding> bindings)
			: base(type)
		{
			this.Bindings = new ReadOnlyCollection<MemberBinding>(bindings.ToList());
		}
	}
}
