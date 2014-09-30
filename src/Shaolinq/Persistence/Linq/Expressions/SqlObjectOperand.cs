// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlObjectOperand
		: SqlBaseExpression
	{
		public ReadOnlyCollection<Expression> ExpressionsInOrder { get; private set; }
		public Dictionary<string, Expression> ExpressionsByPropertyName { get; private set; }
		public Dictionary<Expression, string> PropertyNamesByExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ObjectOperand;
			}
		}

		public SqlObjectOperand(Type type, IList<Expression> expressionsInOrder, IList<string> propertyNames)
			: base(type)
		{
			this.ExpressionsInOrder = expressionsInOrder as ReadOnlyCollection<Expression>;

			if (this.ExpressionsInOrder == null)
			{
				this.ExpressionsInOrder = new ReadOnlyCollection<Expression>(expressionsInOrder);
			}

			this.ExpressionsByPropertyName = new Dictionary<string, Expression>();
			this.PropertyNamesByExpression = new Dictionary<Expression, string>();

			for (var i = 0; i < expressionsInOrder.Count; i++)
			{
				this.ExpressionsByPropertyName[propertyNames[i]] = expressionsInOrder[i];
				this.PropertyNamesByExpression[expressionsInOrder[i]] = propertyNames[i];
			}
		}
	}
}
