using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shaolinq.Persistence.Linq
{
	public struct PropertyPath
		: IEnumerable<PropertyInfo>
	{
		public static readonly PropertyPath Empty = new PropertyPath(new PropertyInfo[0]);

		public PropertyInfo[] Path { get; private set; }
		public int Length { get { return this.Path.Length;  } }
		public PropertyInfo First { get { return this.Path[0];  } }
		public PropertyInfo Last { get { return this.Path[this.Length - 1]; } }
		public PropertyInfo this[int index] { get { return this.Path[index]; } }

		public PropertyPath PathWithoutLast()
		{
			return new PropertyPath(this.Path.Take(this.Length - 1));
		}

		public PropertyPath(params PropertyInfo[] path)
			: this()
		{
			this.Path = path;
		}

		public PropertyPath(IEnumerable<PropertyInfo> path)
			: this()
		{
			this.Path = path.ToArray();
		}

		public IEnumerator<PropertyInfo> GetEnumerator()
		{
			foreach (var part in this.Path)
			{
				yield return part;
			}
		}

		public override string ToString()
		{
			return string.Join(",", this.Path.Select(c => c.Name));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}