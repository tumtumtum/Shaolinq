using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class MethodInvocationRewriter : CSharpSyntaxRewriter
	{
		private readonly ILogger log;
		private readonly SemanticModel model;
		private readonly HashSet<ITypeSymbol> excludeTypes;
		private readonly ITypeSymbol cancellationTokenSymbol;
		
		public MethodInvocationRewriter(ILogger log, SemanticModel model, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
		{
			this.log = log;
			this.model = model;
			this.cancellationTokenSymbol = cancellationTokenSymbol;
			this.excludeTypes = excludeTypes;
		}

		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var syncSymbol = (IMethodSymbol)ModelExtensions.GetSymbolInfo(this.model, node).Symbol;

			if (syncSymbol == null)
			{
				return node;
			}

			var cancellationTokenPos = -1;
			
			if (syncSymbol.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")))
			{
				cancellationTokenPos = syncSymbol.Parameters.TakeWhile(p => !p.IsOptional && !p.IsParams).Count();
			}
			else
			{
				if (this.excludeTypes.Contains(syncSymbol.ContainingType))
				{
					return node;
				}

				var asyncCandidates = syncSymbol
					.ContainingType
					.GetMembers()
					.Where(c => Regex.IsMatch(c.Name, syncSymbol.Name + "Async" + @"(`[0-9])?"))
					.OfType<IMethodSymbol>()
					.ToList();
				
				foreach (var candidate in asyncCandidates.Where(c => c.Parameters.Length == (syncSymbol.IsExtensionMethod ? syncSymbol.Parameters.Length + 2 : syncSymbol.Parameters.Length + 1)))
				{
					var pos = candidate.Parameters.TakeWhile(p => p.Type != this.cancellationTokenSymbol).Count();

					if (pos == candidate.Parameters.Length)
					{
						continue;
					}

					var parameters = candidate.Parameters;

					if (syncSymbol.IsExtensionMethod)
					{
						parameters = parameters.RemoveAt(pos).RemoveAt(0);
						pos--;
					}
					else
					{
						parameters = parameters.RemoveAt(pos);
					}

					if (!parameters.SequenceEqual(syncSymbol.Parameters, ParameterComparer.Default))
					{
						continue;
					}

					cancellationTokenPos = pos;
				}

				if (cancellationTokenPos == -1)
				{
					if (asyncCandidates.Any(ms => ms.Parameters.Length == (syncSymbol.IsExtensionMethod ? syncSymbol.Parameters.Length + 1 : syncSymbol.Parameters.Length) &&
												  (syncSymbol.IsExtensionMethod ? ms.Parameters.Skip(1) : ms.Parameters).SequenceEqual(syncSymbol.Parameters, ParameterComparer.Default)))
					{
						cancellationTokenPos = -1;
					}
					else
					{
						return node;
					}
				}
			}

			var rewritten = this.RewriteExpression(node, cancellationTokenPos);

			if (!(node.Parent is StatementSyntax))
			{
				rewritten = SyntaxFactory.ParenthesizedExpression(rewritten);
			}

			return rewritten;
		}

		ExpressionSyntax RewriteExpression(InvocationExpressionSyntax node, int cancellationTokenPos)
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

				rewrittenInvocation = node.WithExpression(memberAccessExp.WithName(memberAccessExp.Name.WithIdentifier(SyntaxFactory.Identifier(memberAccessExp.Name.Identifier.Text + "Async"))));
			}
			else if (node.Expression is GenericNameSyntax)
			{
				var genericNameExp = (GenericNameSyntax)node.Expression;

				rewrittenInvocation = node.WithExpression(genericNameExp.WithIdentifier(SyntaxFactory.Identifier(genericNameExp.Identifier.Text + "Async")));
			}
			else
			{
				throw new NotSupportedException($"It seems there's an expression type ({node.Expression.GetType().Name}) not yet supported by the AsyncRewriter");
			}

			if (cancellationTokenPos != -1)
			{
				var cancellationTokenArg = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"));

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

			return SyntaxFactory.AwaitExpression(methodInvocation);
		}
	}
}