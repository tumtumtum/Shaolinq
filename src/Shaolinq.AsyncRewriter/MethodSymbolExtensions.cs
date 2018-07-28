// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shaolinq.AsyncRewriter
{
	public static class MethodSymbolExtensions
	{
		public static bool HasRewriteAsyncApplied(this IMethodSymbol self)
		{
			return self.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync"))
					|| self.ContainingType.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync"));
		}

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
