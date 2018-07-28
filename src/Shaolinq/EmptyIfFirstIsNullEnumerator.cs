// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using Shaolinq.Persistence;

namespace Shaolinq
{
	internal partial class EmptyIfFirstIsNullEnumerator<T>
		: IAsyncEnumerator<T>
	{
		private int state;
		private readonly IAsyncEnumerator<T> enumerator;

		public EmptyIfFirstIsNullEnumerator(IAsyncEnumerator<T> enumerator)
		{
			this.enumerator = enumerator;
		}

		public void Dispose()
		{
			this.enumerator.Dispose();
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public T Current { get; private set; }
		object IEnumerator.Current => this.Current;

		[RewriteAsync]
		public bool MoveNext()
		{
			switch (this.state)
			{
			case 0:
				goto state0;
			case 1:
				goto state1;
			case 9:
				goto state9;
			}

state0:
			var result = this.enumerator.MoveNext();

			if (!result || this.enumerator.Current == null)
			{
				this.state = 9;

				return false;
			}
			else
			{
				this.state = 1;

				this.Current = this.enumerator.Current;

				return true;
			}

state1:

			result = this.enumerator.MoveNext();

			if (result)
			{
				this.Current = this.enumerator.Current;

				return true;
			}
			else
			{
				this.state = 9;

				return false;
			}

state9:

			return false;
		}
	}
}