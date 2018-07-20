// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SqlQueryFormatterManager
	{
		public struct FormatParamValue
		{
			public object Value { get; set; }
			public bool AutoQuote { get; set; }

			public FormatParamValue(object value, bool autoQuote)
			{
				this.Value = value;
				this.AutoQuote = autoQuote;
			}
		}

		private volatile Regex formatCommandRegex;
		private readonly SqlDialect sqlDialect;
		private readonly string parameterPrefix;
		private readonly NamingTransformsConfiguration namingTransformsConfiguration;

		public abstract SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default, IDbConnection connection = null);
		
		protected SqlQueryFormatterManager(SqlDialect sqlDialect, NamingTransformsConfiguration namingTransformsConfiguration)
		{
			this.sqlDialect = sqlDialect;
			this.namingTransformsConfiguration = namingTransformsConfiguration;
			this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringEscape);
			this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringQuote); 
			this.parameterPrefix = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default, IDbConnection connection = null)
		{
			return CreateQueryFormatter(options, connection).Format(expression);
		}

		public virtual string GetDefaultValueConstraintName(PropertyDescriptor propertyDescriptor)
		{	
			return VariableSubstituter.SedTransform("", this.namingTransformsConfiguration?.DefaultValueConstraintName ?? NamingTransformsConfiguration.DefaultDefaultValueConstraintName, propertyDescriptor);
		}

		public virtual string GetIndexConstraintName(TypeDescriptor typeDescriptor, string indexName, PropertyDescriptor[] properties)
		{
			return VariableSubstituter.SedTransform(indexName, this.namingTransformsConfiguration?.IndexConstraintName ?? NamingTransformsConfiguration.DefaultIndexConstraintName, properties);
		}

		public virtual string GetForeignKeyConstraintName(PropertyDescriptor propertyDescriptor)
		{
			return VariableSubstituter.SedTransform("", this.namingTransformsConfiguration?.ForeignKeyConstraintName ?? NamingTransformsConfiguration.DefaultForeignKeyConstraintName, propertyDescriptor);
		}

		public virtual string GetPrimaryKeyConstraintName(TypeDescriptor declaringTypeDescriptor, PropertyDescriptor[] primaryKeys)
		{
			return VariableSubstituter.SedTransform("", this.namingTransformsConfiguration?.PrimaryKeyConstraintName ?? NamingTransformsConfiguration.DefaultPrimaryKeyConstraintName, primaryKeys);
		}
		
		public virtual string SubstitutedParameterValues(string commandText, Func<string, Func<object, string>, string> paramNameToString)
		{
			if (this.formatCommandRegex == null)
			{
				this.formatCommandRegex = new Regex($@"\{this.parameterPrefix}{SqlQueryFormatter.ParamNamePrefix}[0-9]+", RegexOptions.Compiled);
			}

			var formatter = CreateQueryFormatter();
			
			return this.formatCommandRegex.Replace(commandText, match => paramNameToString(match.Value, c => formatter.FormatConstant(c)));
		}	
	}
}
