// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SqlQueryFormatterManager
	{
		private volatile Regex formatCommandRegex;
		private readonly SqlDialect sqlDialect;
		private readonly string stringQuote; 
		private readonly string parameterPrefix;
		private readonly string stringEscape;

		public abstract SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default);
		
		protected SqlQueryFormatterManager(SqlDialect sqlDialect)
		{
			this.sqlDialect = sqlDialect;
			this.stringEscape = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringEscape);
			this.stringQuote = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringQuote); 
			this.parameterPrefix = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default)
		{
			return this.CreateQueryFormatter(options).Format(expression);
		}
		
		public virtual string Format(string commandText, Func<string, object> paramNameToValue)
		{
			if (this.formatCommandRegex == null)
			{
				this.formatCommandRegex = new Regex($@"\{this.parameterPrefix}{Sql92QueryFormatter.ParamNamePrefix}[0-9]+", RegexOptions.Compiled);
			}
			
			return this.formatCommandRegex.Replace(commandText, match =>
			{
				var value = paramNameToValue(match.Value);

				if (value == null || value == DBNull.Value)
				{
					return this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Null);
				}

				var type = value.GetType();

				type = Nullable.GetUnderlyingType(type) ?? type;

				if (type == typeof(string) || type.IsEnum)
				{
					var str = value.ToString();

					if (str.Contains(this.stringQuote))
					{
						return this.stringQuote + str.Replace(this.stringQuote, this.stringEscape + this.stringQuote) + this.stringQuote;
					}

					return this.stringQuote + str + this.stringQuote;
				}

				if (type == typeof(Guid))
				{
					var guidValue = (Guid)value;

					return this.stringQuote + guidValue.ToString("D") + this.stringQuote;
				}

				if (type == typeof(TimeSpan))
				{
					var timespanValue = (TimeSpan)value;

					return this.stringQuote + timespanValue + this.stringQuote;
				}

				if (type == typeof(DateTime))
				{
					var dateTime = ((DateTime)value).ToUniversalTime();

					return this.stringQuote + dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffff") + this.stringQuote;
				}

				return Convert.ToString(value);
			});
		}
	}
}
