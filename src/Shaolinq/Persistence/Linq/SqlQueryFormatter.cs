// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Platform;

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
		protected List<TypedValue> parameterValues;
		internal int IndentationWidth { get; }
		public string ParameterIndicatorPrefix { get; protected set; }
		protected bool canReuse = true;
		protected List<Pair<int, int>> parameterIndexToPlaceholderIndexes;
		
		protected readonly SqlDialect sqlDialect;

		public virtual SqlQueryFormatResult Format(Expression expression)
		{
			this.depth = 0;
			this.canReuse = true;
			this.writer = new StringWriter(new StringBuilder(1024));
			this.parameterValues = new List<TypedValue>();
			this.parameterIndexToPlaceholderIndexes = new List<Pair<int, int>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(this, this.writer.ToString(), this.parameterValues, canReuse ? parameterIndexToPlaceholderIndexes : null);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, TextWriter writer)
		{
			this.depth = 0;
			this.canReuse = true;
			this.writer = writer;
			this.parameterValues = new List<TypedValue>();
			this.parameterIndexToPlaceholderIndexes = new List<Pair<int, int>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(this,null, this.parameterValues, canReuse ? parameterIndexToPlaceholderIndexes : null);
		}

		protected SqlQueryFormatter(SqlDialect sqlDialect, TextWriter writer)
		{
			this.sqlDialect = sqlDialect ?? new SqlDialect();
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

			for (var i = 0; i < this.depth * this.IndentationWidth; i++)
			{
				this.writer.Write(' ');
			}
		}

		public virtual void WriteLine(object line)
		{
			this.writer.Write(line);
			this.writer.WriteLine();

			for (var i = 0; i < this.depth * this.IndentationWidth; i++)
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

		protected void WriteDeliminatedListOfItems(IEnumerable listOfItems, Action<object> action, string deliminator = ", ")
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

		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Action<T> action, string deliminator = ", ")
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
		
		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Action<T> action, Action deliminationAction)
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
