// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public string CommandText { get; }
		public IReadOnlyList<TypedValue> ParameterValues { get; }
		public List<Pair<int, int>> ParameterIndexToPlaceholderIndexes { get; }
		
		public SqlQueryFormatResult(string commandText, IEnumerable<TypedValue> parameterValues, List<Pair<int, int>> parameterIndexToPlaceholderIndexes)
			: this(commandText, parameterValues.ToReadOnlyCollection(), parameterIndexToPlaceholderIndexes)
		{	
		}

        public SqlQueryFormatResult(string commandText, IReadOnlyList<TypedValue> parameterValues, List<Pair<int, int>> parameterIndexToPlaceholderIndexes)
		{
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
	        this.ParameterIndexToPlaceholderIndexes = parameterIndexToPlaceholderIndexes;
		}

		public SqlQueryFormatResult ChangeParameterValues(IEnumerable<TypedValue> values)
		{
			return new SqlQueryFormatResult(this.CommandText, values.ToReadOnlyCollection(), this.ParameterIndexToPlaceholderIndexes);
		}

		public SqlQueryFormatResult ChangeParameterValues(object[] values)
		{
			return new SqlQueryFormatResult(this.CommandText, this.ParameterValues.Select((c, i) => new TypedValue(c.Type, values[i])), this.ParameterIndexToPlaceholderIndexes);
		}
	}
}
