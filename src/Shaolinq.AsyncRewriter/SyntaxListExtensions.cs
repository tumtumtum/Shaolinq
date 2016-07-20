using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public static class SyntaxListExtensions
	{
		public static SyntaxList<UsingDirectiveSyntax> Sort(this SyntaxList<UsingDirectiveSyntax> list, bool systemNamespacesFirst = true)
		{
			return SyntaxFactory.List
			(
				list
				.OrderBy(x => x.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? 3 : x.Alias == null ? 1 : 2)
				.ThenByDescending(x => systemNamespacesFirst && x.Name.ToString() == "System" || x.Name.ToString().StartsWith(nameof(System)))
				.ThenBy(c => c.Name.ToString().Length)
				.ThenBy(x => x.Alias?.ToString())
				.ThenBy(x => x.Name.ToString())
			);
		}
	}
}
