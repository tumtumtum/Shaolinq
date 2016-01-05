// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq
{
	public struct ExecutionBuildResult
	{
		public object[] Arguments { get; set; }
		public Delegate Projector { get; set; }
		public SqlQueryFormatResult FormatResult { get; set; }

		public ExecutionBuildResult(SqlQueryFormatResult formatResult, Delegate projector, object[] arguments)
			: this()
		{
			this.Arguments = arguments;
			this.Projector = projector;
			this.FormatResult = formatResult;
		}

		public T Evaluate<T>()
		{
			return ((Func<SqlQueryFormatResult, object[], T>)this.Projector)(this.FormatResult, this.Arguments);
		}
	}
}