// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTypeExpression
		: SqlBaseExpression
	{
		public string TypeName { get; private set; }
		public bool UserDefinedType { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Type;
			}
		}

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
