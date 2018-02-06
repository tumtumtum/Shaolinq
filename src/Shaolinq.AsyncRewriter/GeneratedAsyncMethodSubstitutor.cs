// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class GeneratedAsyncMethodSubstitutor : MethodInvocationInspector
	{
		public GeneratedAsyncMethodSubstitutor(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
			: base(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax)
		{
		}

		public static MethodDeclarationSyntax Rewrite(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
		{
			return (MethodDeclarationSyntax)new GeneratedAsyncMethodSubstitutor(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax).Visit(methodSyntax);
		}

		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			if (this.lambdaStack.Any())
			{
				return node;
			}

			if (node.Expression.Kind() == SyntaxKind.IdentifierName)
			{
				if ((node.Expression as IdentifierNameSyntax)?.Identifier.Text == "nameof")
				{
					return node;
				}
			}
			
			var result = ModelExtensions.GetSpeculativeSymbolInfo(this.semanticModel, node.SpanStart + this.displacement, node, SpeculativeBindingOption.BindAsExpression);

			if (result.Symbol == null)
			{
				var newNode = node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.ArgumentList.Arguments
						.Where(c => this.GetArgumentType(c) != this.cancellationTokenSymbol))));

				var visited = this.Visit(node.Expression);

				if (visited is MemberAccessExpressionSyntax)
				{
					var memberAccess = (MemberAccessExpressionSyntax)visited;

					if (memberAccess.Name.Identifier.Text.EndsWith("Async"))
					{
						var newExp = memberAccess.WithName(SyntaxFactory.IdentifierName(Regex.Replace(memberAccess.Name.Identifier.Text, "Async$", "")));

						newNode = newExp == null ? null : newNode.WithExpression(newExp);
					}
					else
					{
						newNode = null;
					}
				}
				else if (visited is IdentifierNameSyntax)
				{
					var identifier = (IdentifierNameSyntax)visited;

					if (identifier.Identifier.Text.EndsWith("Async"))
					{
						var newExp = identifier.WithIdentifier(SyntaxFactory.Identifier(Regex.Replace(identifier.Identifier.Text, "Async$", "")));

						newNode = newExp == null ? null : newNode.WithExpression(newExp);
					}
					else
					{
						newNode = null;
					}
				}
				else
				{
					newNode = null;
				}

				IMethodSymbol syncMethod;

				if (newNode != null
					&& (syncMethod = (IMethodSymbol)ModelExtensions.GetSpeculativeSymbolInfo((this.semanticModel.ParentModel ?? this.semanticModel), node.SpanStart, newNode, SpeculativeBindingOption.BindAsExpression).Symbol) != null)
				{
					if (syncMethod.HasRewriteAsyncApplied())
					{
						var defaultExpression = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName($"Task<" + syncMethod.ReturnType + ">"));

						return defaultExpression;
					}
				}
			}

			return node
					.WithExpression((ExpressionSyntax)this.Visit(node.Expression))
					.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));
		}

		protected override ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall, int candidateCount)
		{
			return node;
		}
	}
}