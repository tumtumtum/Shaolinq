using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class MethodInvocationAsyncRewriter : MethodInvocationInspector
	{
		public MethodInvocationAsyncRewriter(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
			: base(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol)
		{
		}

		protected override ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall)
		{
			InvocationExpressionSyntax rewrittenInvocation;

			if (node.Expression is IdentifierNameSyntax)
			{
				var identifierName = (IdentifierNameSyntax)node.Expression;

				rewrittenInvocation = node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(identifierName.Identifier.Text + "Async")));
			}
			else if (node.Expression is MemberAccessExpressionSyntax)
			{
				var memberAccessExp = (MemberAccessExpressionSyntax)node.Expression;
				var nestedInvocation = memberAccessExp.Expression as InvocationExpressionSyntax;

				if (nestedInvocation != null)
				{
					memberAccessExp = memberAccessExp.WithExpression((ExpressionSyntax)this.VisitInvocationExpression(nestedInvocation));
				}

				if (explicitExtensionMethodCall)
				{
					rewrittenInvocation = node.WithExpression
					(
						memberAccessExp
							.WithExpression(SyntaxFactory.IdentifierName(candidate.ContainingType.ToMinimalDisplayString(this.semanticModel, node.SpanStart)))
							.WithName(memberAccessExp.Name.WithIdentifier(SyntaxFactory.Identifier(memberAccessExp.Name.Identifier.Text + "Async")))
					);

					rewrittenInvocation = rewrittenInvocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>().Add(SyntaxFactory.Argument(memberAccessExp.Expression.WithoutTrivia())).AddRange(node.ArgumentList.Arguments)));
				}
				else
				{
					rewrittenInvocation = node.WithExpression(memberAccessExp.WithName(memberAccessExp.Name.WithIdentifier(SyntaxFactory.Identifier(memberAccessExp.Name.Identifier.Text + "Async"))));
				}
			}
			else if (node.Expression is GenericNameSyntax)
			{
				var genericNameExp = (GenericNameSyntax)node.Expression;

				rewrittenInvocation = node.WithExpression(genericNameExp.WithIdentifier(SyntaxFactory.Identifier(genericNameExp.Identifier.Text + "Async")));
			}
			else if (node.Expression is MemberBindingExpressionSyntax && node.Parent is ConditionalAccessExpressionSyntax)
			{
				var memberBindingExpression = (MemberBindingExpressionSyntax)node.Expression;

				rewrittenInvocation = node.WithExpression(memberBindingExpression.WithName(memberBindingExpression.Name.WithIdentifier(SyntaxFactory.Identifier(memberBindingExpression.Name.Identifier.Text + "Async"))));
			}
			else
			{
				throw new InvalidOperationException($"Cannot process node of type: ({node.Expression.GetType().Name})");
			}

			if (cancellationTokenPos != -1)
			{
				var cancellationTokenArg = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"));

				if (explicitExtensionMethodCall)
				{
					cancellationTokenPos++;
				}
				
				if (cancellationTokenPos == rewrittenInvocation.ArgumentList.Arguments.Count)
				{
					rewrittenInvocation = rewrittenInvocation.WithArgumentList(rewrittenInvocation.ArgumentList.AddArguments(cancellationTokenArg));
				}
				else
				{
					rewrittenInvocation = rewrittenInvocation.WithArgumentList(SyntaxFactory.ArgumentList(rewrittenInvocation.ArgumentList.Arguments.Insert(cancellationTokenPos, cancellationTokenArg)));
				}
			}

			var methodInvocation = SyntaxFactory.InvocationExpression
			(
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, rewrittenInvocation, SyntaxFactory.IdentifierName("ConfigureAwait")),
				SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().Add(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("false"))))
			);

			var rewritten = (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodInvocation);

			if (!(node.Parent is StatementSyntax))
			{
				rewritten = SyntaxFactory.ParenthesizedExpression(rewritten);
			}

			return rewritten;
		}
		
		public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
		{
			var result = base.VisitConditionalAccessExpression(node);

			if (result != node && ((result as ConditionalAccessExpressionSyntax)?.WhenNotNull as ParenthesizedExpressionSyntax)?.Expression.Kind() == SyntaxKind.AwaitExpression)
			{
				var conditionalAccess = result as ConditionalAccessExpressionSyntax;
				var awaitExpression = (AwaitExpressionSyntax)(conditionalAccess.WhenNotNull as ParenthesizedExpressionSyntax)?.Expression;
				var awaitExpressionExpression = awaitExpression.Expression;

				return SyntaxFactory.AwaitExpression(conditionalAccess.WithWhenNotNull(awaitExpressionExpression));
			}

			return result;
		}
	}
}