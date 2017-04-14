// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
		public SqlOrganizationIndexExpression OrganizationIndex { get; }

		public SqlCreateTableExpression(SqlTableExpression table, bool ifNotExist, IReadOnlyList<SqlColumnDefinitionExpression> columnExpressions, IReadOnlyList<SqlConstraintExpression> tableConstraintExpressions, SqlOrganizationIndexExpression organizationIndex, IReadOnlyList<SqlTableOption> tableOptions = null)
			: base(typeof(void))
		{
			this.Table = table;
			this.IfNotExist = ifNotExist;
			this.TableOptions = tableOptions ?? Enumerable.Empty<SqlTableOption>().ToReadOnlyCollection();
			this.TableConstraints = tableConstraintExpressions;
			this.OrganizationIndex = organizationIndex;
			this.ColumnDefinitionExpressions = columnExpressions;
		}

		public SqlCreateTableExpression ChangeConstraints(IReadOnlyList<SqlConstraintExpression> constraints)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, constraints, this.OrganizationIndex, this.TableOptions);
		}

		public SqlCreateTableExpression ChangeOptions(IReadOnlyList<SqlTableOption> tableOptions)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, this.TableConstraints, this.OrganizationIndex, tableOptions);
		}

		public SqlCreateTableExpression ChangeOrganizationIndex(SqlOrganizationIndexExpression organizationIndex)
		{
			return new SqlCreateTableExpression(this.Table, this.IfNotExist, this.ColumnDefinitionExpressions, this.TableConstraints, organizationIndex, this.TableOptions);
		}
	}
}
