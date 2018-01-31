// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class MethodInvocationAsyncRewriter : MethodInvocationInspector
	{
		protected MethodInvocationAsyncRewriter(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
			: base(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax)
		{
		}

		public static MethodDeclarationSyntax Rewrite(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
		{
			return (MethodDeclarationSyntax)new MethodInvocationAsyncRewriter(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax).Visit(methodSyntax);
		}

		protected override ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall)
		{
			InvocationExpressionSyntax rewrittenInvocation;

			if (node.Expression is IdentifierNameSyntax identifierName)
			{
				rewrittenInvocation = node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(identifierName.Identifier.Text + "Async")));
			}
			else if (node.Expression is MemberAccessExpressionSyntax)
			{
				var memberAccessExp = (MemberAccessExpressionSyntax)node.Expression;


				if (memberAccessExp.Expression is InvocationExpressionSyntax nestedInvocation)
				{
					memberAccessExp = memberAccessExp.WithExpression(nestedInvocation);
				}

				if (explicitExtensionMethodCall)
				{
					rewrittenInvocation = node.WithExpression
					(
						memberAccessExp
							.WithExpression(SyntaxFactory.IdentifierName(candidate.ContainingType.ToMinimalDisplayString(this.semanticModel, node.SpanStart + this.displacement)))
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

			rewritten = SyntaxFactory.ParenthesizedExpression(rewritten);

			return rewritten;
		}

		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
		{
			var expression = this.Visit(node.Expression);

			if (expression is IfStatementSyntax)
			{
				return expression;
			}
			else
			{
				var semicolonToken = this.VisitToken(node.SemicolonToken);

				return node.Update((ExpressionSyntax)expression, semicolonToken);
			}
		}

		public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
		{
			var result = base.VisitConditionalAccessExpression(node);
			var conditionalAccessResult = result as ConditionalAccessExpressionSyntax;

			if (conditionalAccessResult == node || conditionalAccessResult == null)
			{
				return node;
			}

			if (((conditionalAccessResult.WhenNotNull as ParenthesizedExpressionSyntax)?.Expression ?? conditionalAccessResult.WhenNotNull as AwaitExpressionSyntax)?.Kind() == SyntaxKind.AwaitExpression)
			{
				var awaitExpression = (AwaitExpressionSyntax)(conditionalAccessResult.WhenNotNull as ParenthesizedExpressionSyntax)?.Expression ?? conditionalAccessResult.WhenNotNull as AwaitExpressionSyntax;
				var awaitExpressionExpression = awaitExpression?.Expression;

				if (awaitExpressionExpression == null)
				{
					return result;
				}

				var stack = new Stack<ExpressionSyntax>();
				var syntax = awaitExpressionExpression;
				
				while (true)
				{
					if (syntax is MemberAccessExpressionSyntax)
					{
						stack.Push(syntax);
						syntax = ((MemberAccessExpressionSyntax)syntax).Expression;
					}
					else if (syntax is InvocationExpressionSyntax)
					{
						stack.Push(syntax);
						syntax = ((InvocationExpressionSyntax)syntax).Expression;
					}
					else if (syntax is MemberBindingExpressionSyntax)
					{
						var name = ((MemberBindingExpressionSyntax)syntax).Name;

						dynamic current = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, conditionalAccessResult.Expression, name);

						while (stack.Count > 0)
						{
							dynamic next = stack.Pop();

							current = next.WithExpression(current);
						}

						syntax = current;

						break;
					}
					else
					{
						throw new InvalidOperationException("Unsupported expression " + syntax);
					}
				}

				if (node.Parent == null || node.Parent is StatementSyntax)
				{
					return SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, conditionalAccessResult.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.AwaitExpression(syntax))));
				}
				else
				{
					var typeResult = this.semanticModel.GetSpeculativeTypeInfo(node.SpanStart + this.displacement, node, SpeculativeBindingOption.BindAsExpression);

					if (typeResult.Type.IsValueType)
					{
						return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.ConditionalExpression(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, conditionalAccessResult.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), SyntaxFactory.AwaitExpression(syntax), SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(typeResult.Type.ToString()), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))));
					}
					else
					{ 
						return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.ConditionalExpression(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, conditionalAccessResult.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), SyntaxFactory.AwaitExpression(syntax), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
					}
				}
			}

			return result;
		}
	}
}