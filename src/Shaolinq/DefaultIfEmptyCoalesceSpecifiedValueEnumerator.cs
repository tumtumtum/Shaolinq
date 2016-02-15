// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
    internal partial class DefaultIfEmptyCoalesceSpecifiedValueEnumerator<T>
        : IAsyncEnumerator<T?>, IEnumerator<T?>
        where T : struct
    {
        private int state;
        private readonly T? specifiedValue;
        private readonly IEnumerator<T?> enumerator;

        public DefaultIfEmptyCoalesceSpecifiedValueEnumerator(IEnumerator<T?> enumerator, T? specifiedValue)
        {
            this.enumerator = enumerator;
            this.specifiedValue = specifiedValue;
        }

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public T? Current { get; private set; }
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

            if (!result)
            {
                this.Current = this.specifiedValue ?? default(T);

                this.state = 9;

                return true;
            }
            else
            {
                this.state = 1;

                this.Current = this.specifiedValue;

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