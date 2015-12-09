// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlObjectReferenceExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<MemberBinding> Bindings { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.ObjectReference;

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

				var expression = assignment.Expression as SqlObjectReferenceExpression;

				if (expression != null)
				{
					foreach (var value in expression.GetBindingsFlattened())
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

		public SqlObjectReferenceExpression(Type type, MemberBinding[] bindings)
			: this(type, bindings.ToReadOnlyCollection())
		{	
		}

		public SqlObjectReferenceExpression(Type type, IEnumerable<MemberBinding> bindings)
			: this(type, bindings.ToReadOnlyCollection())
		{
		}

		public SqlObjectReferenceExpression(Type type, ReadOnlyCollection<MemberBinding> bindings)
			: base(type)
		{
			this.Bindings = bindings;
		}
	}
}
