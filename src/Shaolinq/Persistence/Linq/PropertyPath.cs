using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public struct PropertyPath
		: IEnumerable<PropertyInfo>
	{
		public static readonly PropertyPath Empty = new PropertyPath(new PropertyInfo[0]);

		public int Length { get { return this.path.Length;  } }
		public PropertyInfo First { get { return this.path[0];  } }
		public bool IsEmpty { get { return this.path.Length == 0; } }
		public PropertyInfo Last { get { return this.path[this.Length - 1]; } }
		public PropertyInfo this[int index] { get { return this.path[index]; } }

		internal readonly PropertyInfo[] path;

		public PropertyPath PathWithoutLast()
		{
			return new PropertyPath(this.path.Take(this.Length - 1));
		}

		public PropertyPath(params PropertyInfo[] path)
			: this()
		{
			this.path = path;
		}

		public PropertyPath(IEnumerable<PropertyInfo> path)
			: this()
		{
			this.path = path.ToArray();
		}

		public IEnumerator<PropertyInfo> GetEnumerator()
		{
			foreach (var part in this.path)
			{
				yield return part;
			}
		}

		public override bool Equals(object obj)
		{
			var value = obj as PropertyPath?;

			return value != null && ArrayEqualityComparer<PropertyInfo>.Default.Equals(this.path, value.Value.path);
		}

		public override int GetHashCode()
		{
			return this.path.Aggregate(this.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override string ToString()
		{
			return string.Join(".", this.path.Select(c => c.Name));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}