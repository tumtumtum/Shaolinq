using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerUniqueIdentifierSqlDataType
		: SqlDataType
	{
		private static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });

		public SqlServerUniqueIdentifierSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType)
			: base(constraintDefaultsConfiguration, supportedType)
		{
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			return "UNIQUEIDENTIFIER";
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			if (this.UnderlyingType == null)
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue(), this.SupportedType), this.SupportedType),
					Expression.Call(dataReader, DataRecordMethods.GetGuidMethod, Expression.Constant(ordinal))
				);
			}
			else
			{
				return Expression.Condition
				(
					Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
					Expression.Convert(Expression.Constant(null, typeof(Guid?)), this.SupportedType),
					Expression.New
					(
						NullableGuidConstructor,
						Expression.Call(dataReader, DataRecordMethods.GetGuidMethod, Expression.Constant(ordinal))
					)
				);
			}
		}
	}
}