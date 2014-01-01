// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public abstract class SqlQueryFormatter
		: SqlExpressionVisitor
	{
		public static string PrefixedTableName(string tableNamePrefix, string tableName)
		{
			if (!string.IsNullOrEmpty(tableNamePrefix))
			{
				return tableNamePrefix + tableName;
			}

			return tableName;
		}

		public abstract SqlQueryFormatResult Format();
		public abstract void Write(object value);
		public abstract void WriteFormat(string format, params object[] args);

		protected virtual Expression PreProcess(Expression expression)
		{
			return expression;
		}

		protected void WriteDeliminatedListOfItems(IEnumerable listOfItems, Func<object, object> action, string deliminator = ", ")
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					this.Write(deliminator);
				}

				action(item);
			}
		}

		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Func<T, object> action,  string deliminator = ", ")
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					this.Write(deliminator);
				}

				action(item);
			}
		}
	}
}
