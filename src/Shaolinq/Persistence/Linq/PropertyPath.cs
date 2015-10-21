// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public struct ObjectPath<T>
		: IEnumerable<T>
	{
		public static readonly ObjectPath<T> Empty = new ObjectPath<T>(new T[0]);

		public int Length => this.path.Length;
		public T First => this.path[0];
		public bool IsEmpty => this.path.Length == 0;
		public T Last => this.path[this.Length - 1];
		public T this[int index] => this.path[index];

		internal readonly T[] path;
		private readonly Func<T, string> toString;

		public ObjectPath<T> PathWithoutLast()
		{
			return new ObjectPath<T>(this.toString, this.path.Take(this.Length - 1));
		}

		public ObjectPath(params T[] path)
			: this()
		{
			this.path = path;
			this.toString = c => c.ToString();
		}

		public ObjectPath(IEnumerable<T> path)
			: this()
		{
			this.path = path.ToArray();
			this.toString = c => c.ToString();
		}

		public ObjectPath(Func<T, string> toString, params T[] path)
			: this()
		{
			this.path = path;
			this.toString = toString;
		}

		public ObjectPath(Func<T, string> toString, IEnumerable<T> path)
			: this()
		{
			this.toString = toString;
			this.path = path.ToArray();
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var part in this.path)
			{
				yield return part;
			}
		}

		public override bool Equals(object obj)
		{
			var value = obj as ObjectPath<T>?;

			return value != null && ArrayEqualityComparer<T>.Default.Equals(this.path, value.Value.path);
		}

		public override int GetHashCode()
		{
			return this.path.Aggregate(this.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override string ToString()
		{
			var toStringLocal = this.toString;

			return string.Join(".", this.path.Select(toStringLocal));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}