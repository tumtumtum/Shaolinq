// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTypeExpression
		: SqlBaseExpression
	{
		public string TypeName { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Type;
			}
		}

		public SqlTypeExpression(string typeName)
			: base(typeof(void))
		{
			this.TypeName = typeName;
		}
	}
}
