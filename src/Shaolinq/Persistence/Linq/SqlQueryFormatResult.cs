// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using Platform;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public string CommandText { get; }
		public IReadOnlyList<Pair<Type, object>> ParameterValues { get; }
		
		public SqlQueryFormatResult(string commandText, IEnumerable<Pair<Type, object>> parameterValues)
			: this(commandText, parameterValues.ToReadOnlyList())
		{	
		}

        public SqlQueryFormatResult(string commandText, IReadOnlyList<Pair<Type, object>> parameterValues)
		{
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
		}
	}
}
