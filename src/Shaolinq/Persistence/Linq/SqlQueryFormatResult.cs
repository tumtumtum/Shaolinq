// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public string CommandText { get; }
		public IReadOnlyList<Tuple<Type, object>> ParameterValues { get; }
		
		public SqlQueryFormatResult(string commandText, IEnumerable<Tuple<Type, object>> parameterValues)
			: this(commandText, parameterValues.ToReadOnlyCollection())
		{	
		}

        public SqlQueryFormatResult(string commandText, IReadOnlyList<Tuple<Type, object>> parameterValues)
		{
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
		}

		public SqlQueryFormatResult ChangeParameterValues(object[] values)
		{
			return new SqlQueryFormatResult(this.CommandText, this.ParameterValues.Select((c, i) => new Tuple<Type, object>(c.Item1, values[i])));
		}
	}
}
