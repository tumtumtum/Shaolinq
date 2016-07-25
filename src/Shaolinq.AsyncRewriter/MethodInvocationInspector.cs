using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
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

		private ITypeSymbol GetArgumentType(ArgumentSyntax syntax)
		{
			var symbol = this.semanticModel.GetSymbolInfo(syntax.Expression).Symbol;

			var property = symbol?.GetType().GetProperty("Type");

			if (property != null)
			{
				return property.GetValue(symbol) as ITypeSymbol;
			}
			
			return this.semanticModel.GetTypeInfo(syntax.Expression).Type;
		}

		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var explicitExtensionMethodCall = false;
			var result = this.semanticModel.GetSymbolInfo(node);

			if (result.Symbol == null)
			{
				var newNode = node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.ArgumentList.Arguments
					.Where(c => GetArgumentType(c) != cancellationTokenSymbol))));

				var memberAccess = node.Expression as MemberAccessExpressionSyntax;

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

				log.LogMessage($"Could not find declaration of method '{node}' at {node.GetLocation().GetLineSpan()}");
				log.LogMessage($"exp= {newNode}");

				return node;
			}

			var methodSymbol = (IMethodSymbol)result.Symbol;
			
			var methodParameters = (methodSymbol.ReducedFrom ?? methodSymbol).ExtensionMethodNormalizingParameters().ToArray();

			IMethodSymbol candidate;
			int cancellationTokenPos;
			
			if (methodSymbol.HasRewriteAsyncApplied())
			{
				candidate = methodSymbol;
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

				candidate = asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes, TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default))
					?? asyncCandidates1.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(methodParameters.Select(e => e.Type), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

				if (candidate == null)
				{
					var asyncCandidates2 = extensionMethodLookup.GetExtensionMethods(methodSymbol.Name + "Async", GetInvocationTargetType(node, methodSymbol)).ToList();

					candidate = asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes, TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default))
						?? asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(methodParameters.Select(e => e.Type), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

					if (candidate != null)
					{
						explicitExtensionMethodCall = true;
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

			return this.InspectExpression(node, cancellationTokenPos, candidate, explicitExtensionMethodCall);
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

		protected abstract ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall);
	}
}
 