// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// Represents an access to an sql-projected SQL column.  The access may be to
	/// an outer select in which case the <see cref="SelectAlias"/> property
	/// is the alias given of the outer select.
	/// </summary>
	public class SqlColumnExpression
		: SqlBaseExpression
	{
		/// <summary>
		/// The alias for the table/select-expression that this column references.
		/// </summary>
		public string SelectAlias { get; private set; }

		/// <summary>
		/// The name of the column within the select that this expression represents.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the name of the column with the alias prepended.
		/// </summary>
		public string AliasedName
		{
			get
			{
				if (string.IsNullOrEmpty(this.SelectAlias))
				{
					return this.Name;
				}

				return this.SelectAlias + "." + this.Name;
			}
		}

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Column;
			}
		}

		public SqlColumnExpression(Type type, string alias, string name)
			: base(type)
		{
			this.Name = name; 
			this.SelectAlias = alias;
		}

		public override string ToString()
		{
			return "COLUMN(" + this.AliasedName +  ")";
		}
	}
}
