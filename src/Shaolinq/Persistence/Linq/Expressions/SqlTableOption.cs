// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTableOption
	{
		public string Name { get; }
		public string Value { get; }
		
		public SqlTableOption(string name, string value)
		{
			this.Name = name;
			this.Value = value;
		}

		public override int GetHashCode()
		{
			return this.Name?.GetHashCode() ?? 0 ^ this.Value?.GetHashCode() ?? 0;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SqlTableOption other))
			{
				return false;
			}

			return this.Name == other.Name && this.Value == other.Value;
		}
	}
}
