using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlEnumDefinitionExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<string> Labels { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.EnumDefinition;
			}
		}

		public SqlEnumDefinitionExpression(ReadOnlyCollection<string> labels)
			: base(typeof(void))
		{
			this.Labels = labels;
		}
	}
}
