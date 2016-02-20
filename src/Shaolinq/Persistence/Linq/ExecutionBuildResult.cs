// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Threading;

namespace Shaolinq.Persistence.Linq
{
	public struct ExecutionBuildResult
	{
		public object[] Arguments { get; set; }
		public Delegate Projector { get; set; }
        public Delegate AsyncProjector { get; set; }
        public SqlQueryFormatResult FormatResult { get; set; }

		public ExecutionBuildResult(SqlQueryFormatResult formatResult, Delegate projector, Delegate asyncProjector, object[] arguments)
			: this()
		{
			this.Arguments = arguments;
			this.Projector = projector;
		    this.AsyncProjector = asyncProjector;
		    this.FormatResult = formatResult;
		}

		public T Evaluate<T>()
		{
			return ((Func<SqlQueryFormatResult, object[], T>)this.Projector)(this.FormatResult, this.Arguments);
		}

        public T EvaluateAsync<T>(CancellationToken cancellationToken)
        {
            return ((Func<SqlQueryFormatResult, object[], CancellationToken, T>)this.AsyncProjector)(this.FormatResult, this.Arguments, cancellationToken);
        }
    }
}