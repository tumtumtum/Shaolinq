// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public string CommandText { get; }
		public IReadOnlyList<TypedValue> ParameterValues { get; }
		
		public SqlQueryFormatResult(string commandText, IEnumerable<TypedValue> parameterValues)
			: this(commandText, parameterValues.ToReadOnlyCollection())
		{	
		}

        public SqlQueryFormatResult(string commandText, IReadOnlyList<TypedValue> parameterValues)
		{
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
		}

		public SqlQueryFormatResult ChangeParameterValues(object[] values)
		{
			return new SqlQueryFormatResult(this.CommandText, this.ParameterValues.Select((c, i) => new TypedValue(c.Type, values[i])));
		}
	}
}
