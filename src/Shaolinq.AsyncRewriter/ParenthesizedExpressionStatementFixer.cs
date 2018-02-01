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
			if (node.Expression is ParenthesizedExpressionSyntax syntax)
			{
				var expression = syntax
					.Expression
					.WithLeadingTrivia(node.GetLeadingTrivia())
					.WithTrailingTrivia(node.GetTrailingTrivia());

				return node.WithExpression(expression);
			}

			return base.VisitExpressionStatement(node);
		}
	}
}