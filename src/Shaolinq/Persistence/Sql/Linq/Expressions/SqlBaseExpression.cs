using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public abstract class SqlBaseExpression
		: Expression
	{
		public override Type Type
		{
			get
			{
				return type;
			}
		}
		private readonly Type type;

		protected SqlBaseExpression(Type type)
		{
			this.type = type;
		}

		public override string ToString()
		{
			return this.GetType().Name + ":" + new Sql92QueryFormatter(this, SqlQueryFormatterOptions.Default).Format().CommandText;
		}
	}
}
