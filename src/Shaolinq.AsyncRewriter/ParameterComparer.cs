using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Shaolinq.AsyncRewriter
{
	internal class ParameterComparer : IEqualityComparer<IParameterSymbol>
	{
		public static readonly ParameterComparer Default = new ParameterComparer();

		public bool Equals(IParameterSymbol x, IParameterSymbol y)
		{
			return x.Name.Equals(y.Name) && x.Type.Equals(y.Type);
		}

		public int GetHashCode(IParameterSymbol p)
		{
			return p.GetHashCode();
		}
	}
}