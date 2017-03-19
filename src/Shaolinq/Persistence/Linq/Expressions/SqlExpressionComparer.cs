// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public partial class SqlExpressionComparer
		: SqlExpressionVisitor
	{
		private bool result;
		private object currentObject;
		private SqlExpressionComparerOptions options;
		
		public SqlExpressionComparer(Expression toCompareTo)
		{
			this.result = true;
			this.currentObject = toCompareTo;
		}

		public static bool Equals(Expression left, Expression right)
		{
			return Equals(left, right, SqlExpressionComparerOptions.None);
		}

		public static bool Equals(Expression left, Expression right, SqlExpressionComparerOptions options)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}

			if (left == null || right == null)
			{
				return false;
			}

			var visitor = new SqlExpressionComparer(right) { options = options };
			
			visitor.Visit(left);

			return visitor.result;
		}

		private bool TryGetCurrent<T>(T paramValue, out T current)
			where T : class
		{
			if (!this.result)
			{
				current = null;

				return false;
			}

			if (paramValue == null && this.currentObject == null)
			{
				current = null;

				return false;
			}

			if (paramValue == null || this.currentObject == null)
			{
				this.result = false;
				current = null;
				
				return false;
			}

			current = this.currentObject as T;

			if (current != null)
			{
				return true;
			}

			this.result = false;

			return false;
		}
		
		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			ConstantExpression current;

			if (!this.TryGetCurrent(constantExpression, out current))
			{
				return constantExpression;
			}

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstants) != 0)
			{
				this.result &= constantExpression.Type == current.Type;

				return constantExpression;
			}

			if (!(this.result &= (current.Type == constantExpression.Type)))
			{
				return constantExpression;
			}

			if (typeof(Expression).IsAssignableFrom(current.Type))
			{
				this.result &= Equals((Expression)current.Value, (Expression)constantExpression.Value, this.options);

				return constantExpression;
			}
			else
			{
				this.result &= Object.Equals(current.Value, constantExpression.Value);

				return constantExpression;
			}
		}
		
		protected override IReadOnlyList<Expression> VisitExpressionList(IReadOnlyList<Expression> original)
		{
			return this.VisitExpressionList<Expression>(original);
		}

		protected override IReadOnlyList<T> VisitExpressionList<T>(IReadOnlyList<T> original)
		{
			IReadOnlyList<T> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			if (!(this.result &= (current.Count == original.Count)))
			{
				return original;
			}

			var count = current.Count;

			for (var i = 0; i < count && this.result; i++)
			{
				this.currentObject = current[i];
				this.Visit(original[i]);

				if (!this.result)
				{
					break;
				}
			}

			this.currentObject = current;

			return original;
		}
		
		protected override IReadOnlyList<MemberBinding> VisitBindingList(IReadOnlyList<MemberBinding> original)
		{
			IReadOnlyList<MemberBinding> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			if (!(this.result &= (current.Count == original.Count)))
			{
				return original; 
			}

			var count = current.Count;

			for (var i = 0; i < count && this.result; i++)
			{
				this.currentObject = current[i];
				this.VisitBinding(original[i]);
			}

			this.currentObject = current;

			return original;
		}

		protected override IReadOnlyList<ElementInit> VisitElementInitializerList(IReadOnlyList<ElementInit> original)
		{
			IReadOnlyList<ElementInit> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			if (!(this.result &= (current.Count == original.Count)))
			{
				return original;
			}

			var count = current.Count;

			for (var i = 0; i < count && this.result; i++)
			{
				this.currentObject = current[i];
				this.VisitElementInitializer(original[i]);
			}
			
			this.currentObject = current;

			return original;
		}

		protected virtual LabelTarget VisitLabelTarget(LabelTarget original)
		{
			LabelTarget current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			if (!(this.result &= current.Name == original.Name))
			{
				return original;
			}

			if (!(this.result &= current.Type == original.Type))
			{
				return original;
			}

			this.currentObject = current;

			return original;
		}

		protected virtual IReadOnlyList<T> VisitObjectList<T>(IReadOnlyList<T> original)
		{
			IReadOnlyList<T> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			if (!(this.result &= (current.Count == original.Count)))
			{
				return original;
			}

			this.result &= original.SequenceEqual(current);

			this.currentObject = current;

			return original;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			SqlConstantPlaceholderExpression current;

			if (!this.TryGetCurrent(constantPlaceholder, out current))
			{
				return constantPlaceholder;
			}

			if (!(this.result &= (current.Index == constantPlaceholder.Index)))
			{
				return constantPlaceholder;
			}

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstantPlaceholders) != 0)
			{
				this.result &= current.Type == constantPlaceholder.Type;

				return constantPlaceholder;
			}

			this.currentObject = current.ConstantExpression;
			this.Visit(constantPlaceholder.ConstantExpression);
			this.currentObject = current;

			return constantPlaceholder;
		}

		protected override IReadOnlyList<SqlColumnDeclaration> VisitColumnDeclarations(IReadOnlyList<SqlColumnDeclaration> columns)
		{
			IReadOnlyList<SqlColumnDeclaration> current;

			if (!this.TryGetCurrent(columns, out current))
			{
				return columns;
			}

			if (!(this.result &= (current.Count == columns.Count)))
			{
				return columns;
			}

			var count = current.Count;

			for (var i = 0; i < count && this.result; i++)
			{
				var item1 = current[i];
				var item2 = columns[i];

				if (item1.Name != item2.Name)
				{
					this.result = false;

					break;
				}

				this.currentObject = item1.Expression;
				this.Visit(item2.Expression);
			}

			this.currentObject = current;
			
			return columns;
		}
	}
}

