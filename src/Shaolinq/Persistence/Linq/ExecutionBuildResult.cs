// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Persistence.Linq
{
	public struct ExecutionBuildResult
	{
		public object[] Arguments { get; }
		public Delegate Projector { get; }
		public Delegate AsyncProjector { get; }
		public SqlQueryFormatResult FormatResult { get; }
		public SqlQueryProvider SqlQueryProvider { get; }

		public ExecutionBuildResult(SqlQueryProvider sqlQueryProvider, SqlQueryFormatResult formatResult, Delegate projector, Delegate asyncProjector, object[] arguments)
			: this()
		{
			this.SqlQueryProvider = sqlQueryProvider;
			this.Arguments = arguments;
			this.Projector = projector;
			this.AsyncProjector = asyncProjector;
			this.FormatResult = formatResult;
		}

		public T Evaluate<T>()
		{
			return ((Func<SqlQueryProvider, SqlQueryFormatResult, object[], T>)this.Projector)(this.SqlQueryProvider, this.FormatResult, this.Arguments);
		}

		public Task<T> EvaluateAsync<T>(CancellationToken cancellationToken)
		{
			return ((Func<SqlQueryProvider, SqlQueryFormatResult, object[], CancellationToken, Task<T>>)this.AsyncProjector)(this.SqlQueryProvider, this.FormatResult, this.Arguments, cancellationToken);
		}

		public IAsyncEnumerable<T> EvaluateAsyncEnumerable<T>(CancellationToken cancellationToken)
		{
			return ((Func<SqlQueryProvider, SqlQueryFormatResult, object[], CancellationToken, IAsyncEnumerable<T>>)this.AsyncProjector)(this.SqlQueryProvider, this.FormatResult, this.Arguments, cancellationToken);
		}
	}
}