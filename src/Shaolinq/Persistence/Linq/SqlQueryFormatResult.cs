// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryFormatResult
	{
		public SqlQueryFormatter Formatter { get; }
		public string CommandText { get; }
		public IReadOnlyList<TypedValue> ParameterValues { get; }
	    public Dictionary<int, int> ParameterIndexToPlaceholderIndexes;
	    public bool Cacheable => ParameterIndexToPlaceholderIndexes != null;
	    public Dictionary<int, int> PlaceholderIndexToParameterIndex;
	    
        public SqlQueryFormatResult(SqlQueryFormatter formatter, string commandText, IEnumerable<TypedValue> parameterValues, IReadOnlyList<Pair<int, int>> parameterIndexToPlaceholderIndexes)
			: this(formatter, commandText, parameterValues.ToReadOnlyCollection(), parameterIndexToPlaceholderIndexes)
		{
		}

		public SqlQueryFormatResult(SqlQueryFormatter formatter, string commandText, IReadOnlyList<TypedValue> parameterValues, IReadOnlyList<Pair<int, int>> parameterIndexToPlaceholderIndexes)
		{
			this.Formatter = formatter;
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;

		    if (parameterIndexToPlaceholderIndexes?.Count > 0)
		    {
		        this.ParameterIndexToPlaceholderIndexes = new Dictionary<int, int>();
		        this.PlaceholderIndexToParameterIndex = new Dictionary<int, int>();

		        foreach (var value in parameterIndexToPlaceholderIndexes)
		        {
                    this.ParameterIndexToPlaceholderIndexes[value.Left] = value.Right;
		            this.PlaceholderIndexToParameterIndex[value.Right] = value.Left;
		        }
		    }
		}

        private SqlQueryFormatResult(SqlQueryFormatter formatter, string commandText, IReadOnlyList<TypedValue> parameterValues, Dictionary<int, int> parameterIndexToPlaceholderIndexes, Dictionary<int, int> placeholderIndexToParameterIndexes)
        {
            this.Formatter = formatter;
            this.CommandText = commandText;
            this.ParameterValues = parameterValues;

            this.ParameterIndexToPlaceholderIndexes = parameterIndexToPlaceholderIndexes;
            this.PlaceholderIndexToParameterIndex = placeholderIndexToParameterIndexes;
        }

        public SqlQueryFormatResult ChangeParameterValues(IEnumerable<TypedValue> values)
        {
            return new SqlQueryFormatResult(this.Formatter, this.CommandText, values.ToReadOnlyCollection(), this.ParameterIndexToPlaceholderIndexes, this.PlaceholderIndexToParameterIndex);
        }

		public SqlQueryFormatResult ChangeParameterValues(object[] values)
		{
		    return new SqlQueryFormatResult(this.Formatter, this.CommandText, this.ParameterValues.Select((c, i) => new TypedValue(c.Type, values[i])).ToReadOnlyCollection(), this.ParameterIndexToPlaceholderIndexes, this.PlaceholderIndexToParameterIndex);
		}
	}
}
