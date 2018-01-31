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
				return node.WithExpression(syntax.Expression);
			}

			return base.VisitExpressionStatement(node);
		}
	}
}