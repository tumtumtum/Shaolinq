// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public string CommandText { get; }
		public IReadOnlyList<Tuple<Type, object>> ParameterValues { get; }
		
		public SqlQueryFormatResult(string commandText, IEnumerable<Tuple<Type, object>> parameterValues)
			: this(commandText, parameterValues.ToReadOnlyList())
		{	
		}

        public SqlQueryFormatResult(string commandText, IReadOnlyList<Tuple<Type, object>> parameterValues)
		{
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
		}
	}
}
