// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public enum SqlExpressionType
	{
		First = 8192,
		Table,
		Select,
		Projection,
		Delete,
		Where, // 7
		Limit,
		Column, // 9
		FunctionCall,
		Join,
		Aggregate,
		AggregateSubquery,
		Subquery,
		ObjectOperand,
		ConstantPlaceholder,
		Tuple,
		AlterTable,
		ColumnDefinition,
		CreateIndex,
		CreateTable,
		ForeignKeyConstraint,
		ReferencesColumn,
		SimpleConstraint,
		StatementList
	}
}
