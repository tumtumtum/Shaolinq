using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public class InterpolatedFormatSpecifierFixer
		: CSharpSyntaxRewriter
	{
		public static SyntaxNode Fix(SyntaxNode syntaxNode)
		{
			return new InterpolatedFormatSpecifierFixer().Visit(syntaxNode);
		}

		public override SyntaxNode VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node)
		{
			if (node.ColonToken.HasTrailingTrivia)
			{
				var newTrailingTrivia = node.ColonToken.TrailingTrivia.Where(c => c.Kind() != SyntaxKind.WhitespaceTrivia);

				return node.WithColonToken(node.ColonToken.WithTrailingTrivia(newTrailingTrivia));
			}

			return base.VisitInterpolationFormatClause(node);
		}
	}
}
