using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shaolinq.AsyncRewriter
{
	public static class MethodSymbolExtensions
	{
		public static ITypeSymbol ExtensionMethodNormalizingReceiverType(this IMethodSymbol self)
		{
			return self.IsExtensionMethod && self.ReducedFrom == null ? self.Parameters[0].Type : self.ReceiverType;
		}

		public static IEnumerable<IParameterSymbol> ExtensionMethodNormalizingParameters(this IMethodSymbol self)
		{
			return self.IsExtensionMethod && self.ReducedFrom == null ? self.Parameters.Skip(1) : self.Parameters;
		}
	}
}
