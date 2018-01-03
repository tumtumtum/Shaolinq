// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
		private readonly string stringValue;

		public ObjectPath<T> RemoveLast()
		{
			return new ObjectPath<T>(this.toString, this.path.Take(this.Length - 1));
		}

		public ObjectPath(params T[] path)
			: this()
		{
			this.path = path;
			this.toString = c => c.ToString();
			this.stringValue = string.Join(".", this.path.Select(this.toString));
		}

		public ObjectPath(IEnumerable<T> path)
			: this()
		{
			this.path = path.ToArray();
			this.toString = c => c.ToString();
			this.stringValue = string.Join(".", this.path.Select(this.toString));
		}

		public ObjectPath(Func<T, string> toString, params T[] path)
			: this()
		{
			this.path = path;
			this.toString = toString;
			this.stringValue = string.Join(".", this.path.Select(toString));
		}

		public ObjectPath(Func<T, string> toString, IEnumerable<T> path)
			: this()
		{
			this.toString = toString;
			this.path = path.ToArray();
			this.stringValue = string.Join(".", this.path.Select(toString));
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
			return obj is ObjectPath<T> value && ArrayEqualityComparer<T>.Default.Equals(this.path, value.path);
		}

		public override int GetHashCode()
		{
			return this.path.Aggregate(this.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override string ToString()
		{
			return this.stringValue;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}