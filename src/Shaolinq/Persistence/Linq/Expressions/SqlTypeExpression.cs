// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTypeExpression
		: SqlBaseExpression
	{
		public string TypeName { get; }
		public bool UserDefinedType { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Type;

		public SqlTypeExpression(string typeName)
			: this(typeName, false)
		{	
		}

		public SqlTypeExpression(string typeName, bool userDefinedType)
			: base(typeof(void))
		{
			this.TypeName = typeName;
			this.UserDefinedType = userDefinedType;
		}
	}
}
