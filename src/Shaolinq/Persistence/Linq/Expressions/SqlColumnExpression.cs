// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;

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
		public string Name { get; }
		public bool Special { get; }
		public string SelectAlias { get; }
		public string AliasedName { get; }

		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Column;

		public SqlColumnExpression(Type type, string alias, string name, bool special = false)
			: base(type)
		{
			this.Name = name;
			this.Special = special;
			this.SelectAlias = alias;
			this.Special = special;
			this.AliasedName = string.IsNullOrEmpty(this.SelectAlias) ? this.Name : this.SelectAlias + "." + this.Name;
		}

		public override string ToString()
		{
			return "COLUMN(" + this.AliasedName +  ")";
		}

		public override int GetHashCode()
		{
			return this.SelectAlias.GetHashCode() ^ this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as SqlColumnExpression;

			return other != null && (object.ReferenceEquals(this, other) || (this.SelectAlias == other.SelectAlias && this.Name == other.Name));
		}

		public SqlColumnExpression ChangeToNullable()
		{
			var nullableType = this.Type.MakeNullable();

			return nullableType == this.Type ? this : new SqlColumnExpression(nullableType, this.SelectAlias, this.Name, this.Special);
		}
	}
}
