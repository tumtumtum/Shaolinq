// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

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
		
		public abstract SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default);
		
		protected SqlQueryFormatterManager(SqlDialect sqlDialect)
		{
			this.sqlDialect = sqlDialect;
			this.stringQuote = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringQuote); 
			this.parameterPrefix = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default)
		{
			return CreateQueryFormatter(options).Format(expression);
		}
		
		public virtual string Format(string commandText, Func<string, object> paramNameToValue)
		{
			if (formatCommandRegex == null)
			{
				formatCommandRegex = new Regex(string.Format(@"\{0}" + Sql92QueryFormatter.ParamNamePrefix + "[0-9]+", this.parameterPrefix), RegexOptions.Compiled);
			}

			return formatCommandRegex.Replace(commandText, match =>
			{
				var value = paramNameToValue(match.Value);

				if (value == null)
				{
					return this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Null);
				}

				var type = value.GetType();

				type = Nullable.GetUnderlyingType(type) ?? type;

				if (type == typeof(string) || type.IsEnum)
				{
					return this.stringQuote + value + this.stringQuote;
				}

				if (type == typeof(Guid))
				{
					var guidValue = (Guid)value;

					return this.stringQuote + guidValue.ToString("D") + this.stringQuote;
				}
				
				if (type == typeof(DateTime))
				{
					var dateTime = (DateTime)value;

					return this.stringQuote + dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffff zz") + this.stringQuote;
				}

				return Convert.ToString(value);
			});
		}
	}
}
