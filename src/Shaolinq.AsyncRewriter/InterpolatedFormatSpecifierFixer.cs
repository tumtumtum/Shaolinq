using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public class ParenthesizedExpressionStatementFixer
		: CSharpSyntaxRewriter
	{
		public static SyntaxNode Fix(SyntaxNode syntaxNode)
		{
			return new ParenthesizedExpressionStatementFixer().Visit(syntaxNode);
		}

		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
		{
			if (node.Expression is ParenthesizedExpressionSyntax)
			{
				return node.WithExpression((node.Expression as ParenthesizedExpressionSyntax).Expression);
			}

			return base.VisitExpressionStatement(node);
		}
	}

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
