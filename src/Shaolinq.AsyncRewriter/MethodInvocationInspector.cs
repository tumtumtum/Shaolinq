using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class MethodInvocationRewriter : MethodInvocationInspector
	{
		public MethodInvocationRewriter(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
			: base(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol)
		{
		}
		
		protected override ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, ITypeSymbol typeForExplicitExtensionMethodCall)
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
				throw new InvalidOperationException($"Cannot process node of type: ({node.Expression.GetType().Name})");
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

	internal abstract class MethodInvocationInspector : CSharpSyntaxRewriter
	{
		protected readonly IAsyncRewriterLogger log;
		protected readonly SemanticModel semanticModel;
		protected readonly HashSet<ITypeSymbol> excludeTypes;
		protected readonly ITypeSymbol cancellationTokenSymbol;
		protected readonly CompilationLookup extensionMethodLookup;

		protected MethodInvocationInspector(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup,  SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
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
			var result = this.semanticModel.GetSymbolInfo(node);
			
			if (result.Symbol == null)
			{
				var newNode = node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.ArgumentList.Arguments.Where(c => this.semanticModel.GetTypeInfo(c).Type?.Name != "CancellationToken"))));
				var memberAccess = (node.Expression as MemberAccessExpressionSyntax);

				if (memberAccess != null)
				{
					var newExp = memberAccess?.WithName(SyntaxFactory.IdentifierName(memberAccess.Name.Identifier.Text.Replace("Async", "")));

					newNode = newExp == null ? null : newNode.WithExpression(newExp);
				}
				else
				{
					newNode = null;
				}

				IMethodSymbol syncVersion;

				if (newNode != null 
					&& (syncVersion = (IMethodSymbol)this.semanticModel.GetSpeculativeSymbolInfo(node.SpanStart, newNode, SpeculativeBindingOption.BindAsExpression).Symbol) != null)
				{
					if (syncVersion.HasRewriteAsyncApplied())
					{
						return node;
					}
				}

				log.LogMessage($"Could not find declaration of method '{node}' in {node.SyntaxTree.FilePath}@{node.GetLocation().GetLineSpan()}");

				return node;
			}

			var methodSymbol = (IMethodSymbol)result.Symbol;
			
			var methodParameters = (methodSymbol.ReducedFrom ?? methodSymbol).ExtensionMethodNormalizingParameters().ToArray();

			int cancellationTokenPos;
			
			if (methodSymbol.HasRewriteAsyncApplied())
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

				var candidate = asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes, TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default))
					?? asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type as INamedTypeSymbol).SequenceEqual(methodParameters.Select(e => e.Type as INamedTypeSymbol), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

				if (candidate == null)
				{
					var asyncCandidates2 = extensionMethodLookup.GetExtensionMethods(methodSymbol.Name + "Async", GetInvocationTargetType(node, methodSymbol)).ToList();

					candidate = asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes, TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default))
						?? asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type as INamedTypeSymbol).SequenceEqual(methodParameters.Select(e => e.Type as INamedTypeSymbol), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

					if (candidate != null)
					{
						typeForExplicitExtensionMethodCall = candidate.ContainingType;
					}
					else
					{
						if (result.Symbol.Name == "FirstOrDefault")
						{
							log.LogMessage($"Could not find async version of: {node}");
							log.LogMessage($"InvocationTargetType: {GetInvocationTargetType(node, methodSymbol)}");
							log.LogMessage($"File: {node.SyntaxTree.FilePath}");
							log.LogMessage("Candidates: " + string.Join(";", asyncCandidates2.Select(c => c.ToString())));
						}

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

			return this.InspectExpression(node, cancellationTokenPos, typeForExplicitExtensionMethodCall);
		}

		private ITypeSymbol GetInvocationTargetType(InvocationExpressionSyntax node, IMethodSymbol methodSymbol)
		{
			ITypeSymbol retval;
			var notError = false;

			if (node.Expression is MemberAccessExpressionSyntax)
			{
				retval = this.semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)node.Expression).Expression).Type;
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
					retval = this.semanticModel.GetTypeInfo(((ConditionalAccessExpressionSyntax)node.Parent).Expression).Type;
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
				retval = methodSymbol.ExtensionMethodNormalizingReceiverType();

				if (!notError)
				{
					log.LogError($"Unable to determine type of {node.Expression} in {node.SyntaxTree.FilePath} {node.Expression.GetType()}");
				}
			}

			return retval;
		}

		protected abstract ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, ITypeSymbol typeForExplicitExtensionMethodCall);
	}
}
 