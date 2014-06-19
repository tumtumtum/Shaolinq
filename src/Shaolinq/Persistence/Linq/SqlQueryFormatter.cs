// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public abstract class SqlQueryFormatter
		: SqlExpressionVisitor
	{
		public const char DefaultParameterIndicatorChar = '@';

		protected enum Indentation
		{
			Same,
			Inner,
			Outer
		}

		public class IndentationContext
			: IDisposable
		{
			private readonly Sql92QueryFormatter parent;

			public IndentationContext(Sql92QueryFormatter parent)
			{
				this.parent = parent;
				this.parent.depth++;
				this.parent.WriteLine();
			}

			public void Dispose()
			{
				this.parent.depth--;
			}
		}

		public static string PrefixedTableName(string tableNamePrefix, string tableName)
		{
			if (!string.IsNullOrEmpty(tableNamePrefix))
			{
				return tableNamePrefix + tableName;
			}

			return tableName;
		}

		private int depth;
		protected TextWriter writer;
		protected List<Pair<Type, object>> parameterValues;
		internal int IndentationWidth { get; private set; }
		public string ParameterIndicatorPrefix { get; protected set; }
		protected readonly SqlDialect sqlDialect;

		public virtual SqlQueryFormatResult Format(Expression expression)
		{
			this.writer = new StringWriter(new StringBuilder(1024));
			this.parameterValues = new List<Pair<Type, object>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(this.writer.ToString(), parameterValues);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, TextWriter writer)
		{
			this.writer = writer;
			this.parameterValues = new List<Pair<Type, object>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(null, parameterValues);
		}

		protected SqlQueryFormatter(SqlDialect sqlDialect, TextWriter writer)
		{
			this.sqlDialect = sqlDialect ?? SqlDialect.Default;
			this.writer = writer;
			this.ParameterIndicatorPrefix = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
			this.IndentationWidth = 2;
		}

		protected void Indent(Indentation style)
		{
			if (style == Indentation.Inner)
			{
				this.depth++;
			}
			else if (style == Indentation.Outer)
			{
				this.depth--;
			}
		}

		public virtual void WriteLine()
		{
			this.writer.WriteLine();

			for (var i = 0; i < depth * this.IndentationWidth; i++)
			{
				this.writer.Write(' ');
			}
		}

		public virtual void WriteLine(object line)
		{
			this.writer.Write(line);
			this.writer.WriteLine();

			for (var i = 0; i < depth * this.IndentationWidth; i++)
			{
				this.writer.Write(' ');
			}
		}

		public virtual void Write(object value)
		{
			this.writer.Write(value);
		}

		public virtual void WriteFormat(string format, params object[] args)
		{
			this.writer.Write(format, args);
		}

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

		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Func<T, object> action, Action deliminationAction)
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					deliminationAction();
				}

				action(item);
			}
		}
	}
}
