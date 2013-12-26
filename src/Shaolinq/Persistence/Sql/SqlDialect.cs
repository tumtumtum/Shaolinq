// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Platform;

namespace Shaolinq.Persistence.Sql
{
	public class SqlDialect
	{
		public static readonly SqlDialect Default = new SqlDialect();

		public virtual string DeferrableText
		{
			get
			{
				return "";
			}
		}

		public virtual bool SupportsFeature(SqlFeature feature)
		{
			switch (feature)
			{
				case SqlFeature.AlterTableAddConstraints:
					return true;
				case SqlFeature.Constraints:
					return true;
				case SqlFeature.IndexNameCasing:
					return true;
				case SqlFeature.IndexToLower:
					return true;
				case SqlFeature.SelectForUpdate:
					return true;
				default:
					return false;
			}
		}

		public virtual Pair<string, PropertyDescriptor>[] GetPersistedNames(DataAccessModel dataAccessModel, PropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.IsBackReferenceProperty)
			{
				var i = 0;
				var typeDescriptor = dataAccessModel.GetTypeDescriptor(propertyDescriptor.PropertyType);

				var retval = new Pair<string, PropertyDescriptor>[typeDescriptor.PrimaryKeyProperties.Count];

				foreach (var relatedPropertyDescriptor in typeDescriptor.PrimaryKeyProperties)
				{
					retval[i] = new Pair<string, PropertyDescriptor>(propertyDescriptor.PersistedName + relatedPropertyDescriptor.PersistedShortName, relatedPropertyDescriptor);

					i++;
				}

				return retval;
			}
			else if (propertyDescriptor.PersistedMemberAttribute != null && propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				var i = 0;
				var typeDescriptor = dataAccessModel.GetTypeDescriptor(propertyDescriptor.PropertyType);

				var retval = new Pair<string, PropertyDescriptor>[typeDescriptor.PrimaryKeyProperties.Count];

				foreach (var relatedPropertyDescriptor in typeDescriptor.PrimaryKeyProperties)
				{
					retval[i] = new Pair<string, PropertyDescriptor>(propertyDescriptor.PersistedName + relatedPropertyDescriptor.PersistedShortName, relatedPropertyDescriptor);

					i++;
				}

				return retval;
			}
			else
			{
				return new[] { new Pair<string, PropertyDescriptor>(propertyDescriptor.PersistedName, propertyDescriptor) };
			}
		}

		public virtual string GetColumnDataTypeName(PropertyDescriptor propertyDescriptor, SqlDataType sqlDataType, bool foreignKey)
		{
			return sqlDataType.GetSqlName(propertyDescriptor);
		}

		public virtual string GetSyntaxSymbolString(SqlSyntaxSymbol symbol)
		{
			switch (symbol)
			{
				case SqlSyntaxSymbol.Null:
					return "NULL";
				case SqlSyntaxSymbol.Like:
					return "LIKE";
				case SqlSyntaxSymbol.IdentifierQuote:
					return "\"";
				case SqlSyntaxSymbol.AutoIncrementSuffix:
					return "AUTO_INCREMENT";
				default:
					return "";
			}
		}
	}
}
