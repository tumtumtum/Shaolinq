// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTableExpression
		: SqlBaseExpression
	{
		public bool IfNotExist { get; }
		public SqlTableExpression Table { get; }
		public IReadOnlyList<SqlTableOption> TableOptions { get; }
		public IReadOnlyList<SqlConstraintExpression> TableConstraints { get; }
		public IReadOnlyList<SqlColumnDefinitionExpression> ColumnDefinitionExpressions { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.CreateTable;

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IEnumerable<SqlColumnDefinitionExpression> columnExpressions, IEnumerable<SqlConstraintExpression> tableConstraintExpressions, IReadOnlyList<SqlTableOption> tableOptions = null)
			: this(table, ifNotExist, columnExpressions.ToReadOnlyCollection(), tableConstraintExpressions.ToReadOnlyCollection(), tableOptions)
		{
		}

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IReadOnlyList<SqlColumnDefinitionExpression> columnExpressions, IReadOnlyList<SqlConstraintExpression> tableConstraintExpressions, IReadOnlyList<SqlTableOption> tableOptions = null)
			: base(typeof(void))
		{
			this.Table = table;
			this.IfNotExist = ifNotExist;
			this.TableOptions = tableOptions ?? Enumerable.Empty<SqlTableOption>().ToReadOnlyCollection();
			this.TableConstraints = tableConstraintExpressions;
			this.ColumnDefinitionExpressions = columnExpressions;
		}

		public SqlCreateTableExpression ChangeConstraints(IReadOnlyList<SqlConstraintExpression> constraints)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, constraints, this.TableOptions);
		}

		public SqlCreateTableExpression UpdateOptions(IReadOnlyList<SqlTableOption> tableOptions)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, this.TableConstraints, tableOptions);
		}
	}
}
