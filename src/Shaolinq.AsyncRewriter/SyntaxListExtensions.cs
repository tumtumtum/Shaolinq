using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public static class SyntaxListExtensions
	{
		private class UsingDirectiveSyntaxEqualityComparer
			: IEqualityComparer<UsingDirectiveSyntax>
		{
			public static readonly UsingDirectiveSyntaxEqualityComparer Default = new UsingDirectiveSyntaxEqualityComparer();

			public bool Equals(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
			{
				return x.Name?.ToString() == y.Name?.ToString()
					&& x.Alias?.ToString() == y.Alias?.ToString()
					&& x.StaticKeyword.Kind() == y.StaticKeyword.Kind();
			}

			public int GetHashCode(UsingDirectiveSyntax obj)
			{
				return obj.Name?.ToString().GetHashCode() ?? 0;
			}
		}

		public static SyntaxList<UsingDirectiveSyntax> Sort(this SyntaxList<UsingDirectiveSyntax> list, bool systemNamespacesFirst = true)
		{
			return SyntaxFactory.List
			(
				list
				.Distinct(UsingDirectiveSyntaxEqualityComparer.Default)
				.OrderBy(x => x.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? 3 : x.Alias == null ? 1 : 2)
				.ThenByDescending(x => systemNamespacesFirst && x.Name.ToString() == "System" || x.Name.ToString().StartsWith(nameof(System)))
				.ThenBy(c => c.Name.ToString().Length)
				.ThenBy(x => x.Alias?.ToString())
				.ThenBy(x => x.Name.ToString())
			);
		}
	}
}
