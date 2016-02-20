// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence.Linq.Expressions
{
	public struct SubstituteConstantsResultWithValues
	{
		public object[] Values { get; }
		public SubstituteConstantsResult Result { get; }

		public SubstituteConstantsResultWithValues(SubstituteConstantsResult result, object[] values)
		{
			this.Result = result;
			this.Values = values;
		}
	}
}