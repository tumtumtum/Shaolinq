// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
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

		public virtual string GetDefaultValueConstraintName(PropertyDescriptor propertyDescriptor)
		{
			var defaultName = VariableSubstituter.SedTransform("", NamingTransformsConfiguration.DefaultDefaultValueConstraintName, propertyDescriptor);
			var namingTransformsConfiguration = propertyDescriptor.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms;

			if (namingTransformsConfiguration?.DefaultValueConstraintName == null)
			{
				return defaultName;
			}

			return VariableSubstituter.SedTransform(defaultName, namingTransformsConfiguration.DefaultValueConstraintName);
		}

		public virtual string GetIndexConstraintName(PropertyDescriptor propertyDescriptor)
		{
			var defaultName = VariableSubstituter.SedTransform("", NamingTransformsConfiguration.DefaultIndexConstraintName, propertyDescriptor);
			var namingTransformsConfiguration = propertyDescriptor.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms;

			if (namingTransformsConfiguration?.IndexConstraintName == null)
			{
				return defaultName;
			}

			return VariableSubstituter.SedTransform(defaultName, namingTransformsConfiguration.IndexConstraintName);
		}

		public virtual string GetForeignKeyConstraintName(PropertyDescriptor propertyDescriptor)
		{
			var defaultName = VariableSubstituter.SedTransform("", NamingTransformsConfiguration.DefaultForeignKeyConstraintName, propertyDescriptor);
			var namingTransformsConfiguration = propertyDescriptor.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms;

			if (namingTransformsConfiguration?.ForeignKeyConstraintName == null)
			{
				return defaultName;
			}

			return VariableSubstituter.SedTransform(defaultName, namingTransformsConfiguration.ForeignKeyConstraintName);
		}

		public virtual string GetPrimaryKeyConstraintName(TypeDescriptor declaringTypeDescriptor, PropertyDescriptor[] primaryKeys)
		{
			var defaultName = VariableSubstituter.SedTransform("", NamingTransformsConfiguration.DefaultPrimaryKeyConstraintName, primaryKeys);
			var namingTransformsConfiguration = declaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms;

			if (namingTransformsConfiguration?.PrimaryKeyConstraintName == null)
			{
				return defaultName;
			}

			return VariableSubstituter.SedTransform(defaultName, namingTransformsConfiguration.PrimaryKeyConstraintName);
		}

		public virtual string SubstitutedParameterValues(string commandText, Func<string, Func<object, string>, string> paramNameToString)
		{
			if (this.formatCommandRegex == null)
			{
				this.formatCommandRegex = new Regex($@"\{this.parameterPrefix}{SqlQueryFormatter.ParamNamePrefix}[0-9]+", RegexOptions.Compiled);
			}

			var formatter = this.CreateQueryFormatter();
			
			return this.formatCommandRegex.Replace(commandText, match => paramNameToString(match.Value, c => formatter.FormatConstant(c)));
		}	
	}
}
