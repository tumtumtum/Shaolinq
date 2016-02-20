// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
	}
}
