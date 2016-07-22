using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		private readonly SemanticModel semanticModel;
		private readonly HashSet<ITypeSymbol> excludeTypes;
		private readonly ITypeSymbol cancellationTokenSymbol;
		private readonly CompilationLookup extensionMethodLookup;

		public MethodInvocationRewriter(ILogger log, CompilationLookup extensionMethodLookup,  SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
		{
			this.log = log;
			this.extensionMethodLookup = extensionMethodLookup;
			this.semanticModel = semanticModel;
			this.cancellationTokenSymbol = cancellationTokenSymbol;
			this.excludeTypes = excludeTypes;
		}

		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			ITypeSymbol typeForExplicitExtensionMethodCall = null;
			var methodSymbol = (IMethodSymbol)this.semanticModel.GetSymbolInfo(node).Symbol ?? (IMethodSymbol)this.extensionMethodLookup.GetSymbol(node);
			
			if (methodSymbol == null)
			{
				return node;
			}
			
			var methodParameters = (methodSymbol.ReducedFrom ?? methodSymbol).ExtensionMethodNormalizingParameters().ToArray();
			
			int cancellationTokenPos;
			
			if (methodSymbol.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync"))
				|| methodSymbol.ContainingType.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")))
			{
				cancellationTokenPos = methodParameters.TakeWhile(p => !p.IsOptional && !p.IsParams).Count();
			}
			else
			{
				if (this.excludeTypes.Contains(methodSymbol.ContainingType))
				{
					return node;
				}
				
				var expectedParameterTypes = new List<ITypeSymbol>();
				
				expectedParameterTypes.AddRange(methodParameters.TakeWhile(c => !(c.HasExplicitDefaultValue || c.IsParams)).Select(c => c.Type));
				expectedParameterTypes.Add(this.cancellationTokenSymbol);
				expectedParameterTypes.AddRange(methodParameters.SkipWhile(c => !(c.HasExplicitDefaultValue || c.IsParams)).Select(c => c.Type));
				
				var asyncCandidates1 = methodSymbol
					.ContainingType
					.GetMembers()
					.Where(c => Regex.IsMatch(c.Name, methodSymbol.Name + "Async" + @"(`[0-9])?"))
					.OfType<IMethodSymbol>()
					.ToList();

				var candidate = asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes))
					?? asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type as INamedTypeSymbol).SequenceEqual(methodParameters.Select(e => e.Type as INamedTypeSymbol), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

				if (candidate == null)
				{
					var asyncCandidates2 = extensionMethodLookup.GetExtensionMethods(methodSymbol.Name + "Async", GetInvocationTargetType(node, methodSymbol)).ToList();

					candidate = asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes))
						?? asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type as INamedTypeSymbol).SequenceEqual(methodParameters.Select(e => e.Type as INamedTypeSymbol), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

					if (candidate != null)
					{
						typeForExplicitExtensionMethodCall = candidate.ContainingType;
					}
					else
					{
						return node;
					}
				}

				if (candidate.ExtensionMethodNormalizingParameters().Any(c => c.Type == this.cancellationTokenSymbol))
				{
					cancellationTokenPos = candidate.ExtensionMethodNormalizingParameters().Count(c => c.Type != this.cancellationTokenSymbol);
				}
				else
				{
					cancellationTokenPos = -1;
				}
			}

			var rewritten = this.RewriteExpression(node, cancellationTokenPos, typeForExplicitExtensionMethodCall);

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

		private INamedTypeSymbol GetInvocationTargetType(InvocationExpressionSyntax node, IMethodSymbol methodSymbol)
		{
			var notError = false;
			INamedTypeSymbol retval;

			if (node.Expression is MemberAccessExpressionSyntax)
			{
				retval = (INamedTypeSymbol)this.semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)node.Expression).Expression).Type;
			}
			else if (node.Expression is IdentifierNameSyntax)
			{
				notError = true;
				retval = null;
			}
			else if (node.Expression is MemberBindingExpressionSyntax)
			{
				if (node.Parent is ConditionalAccessExpressionSyntax)
				{
					retval = (INamedTypeSymbol)this.semanticModel.GetTypeInfo(((ConditionalAccessExpressionSyntax)node.Parent).Expression).Type;
				}
				else
				{
					retval = null;
				}
			}
			else
			{
				retval = null;
			}

			if (retval == null)
			{
				retval = (INamedTypeSymbol)methodSymbol.ExtensionMethodNormalizingReceiverType();

				if (!notError)
				{
					Console.WriteLine($"Unable to determine type of {node.Expression} in {node.SyntaxTree.FilePath} {node.Expression.GetType()}");
				}
			}

			return retval;
		}

		ExpressionSyntax RewriteExpression(InvocationExpressionSyntax node, int cancellationTokenPos, ITypeSymbol typeForExplicitExtensionMethodCall)
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

				if (typeForExplicitExtensionMethodCall != null)
				{
					rewrittenInvocation = node.WithExpression
					(
						memberAccessExp
							.WithExpression(SyntaxFactory.IdentifierName(typeForExplicitExtensionMethodCall.ToMinimalDisplayString(this.semanticModel, node.SpanStart)))
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
				throw new NotSupportedException($"It seems there's an expression type ({node.Expression.GetType().Name}) not yet supported by the AsyncRewriter");
			}

			if (cancellationTokenPos != -1)
			{
				var cancellationTokenArg = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"));

				if (typeForExplicitExtensionMethodCall != null)
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
			
			return SyntaxFactory.AwaitExpression(methodInvocation);
		}
	}
}
 