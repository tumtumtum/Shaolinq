using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shaolinq.AsyncRewriter
{
	internal abstract class MethodInvocationInspector
		: CSharpSyntaxRewriter
	{
		private readonly Stack<ExpressionSyntax> lambdaStack = new Stack<ExpressionSyntax>();

		protected SemanticModel semanticModel;
		protected readonly IAsyncRewriterLogger log;
		protected readonly HashSet<ITypeSymbol> excludeTypes;
		protected readonly ITypeSymbol cancellationTokenSymbol;
		protected int displacement;
		private MethodDeclarationSyntax methodSyntax;
		protected readonly CompilationLookup extensionMethodLookup;
		
		protected MethodInvocationInspector(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup,  SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
		{
			this.log = log;
			this.extensionMethodLookup = extensionMethodLookup;
			this.semanticModel = semanticModel;
			this.cancellationTokenSymbol = cancellationTokenSymbol;
			this.methodSyntax = methodSyntax;
			this.excludeTypes = excludeTypes;
		}
		
		private ITypeSymbol GetArgumentType(ArgumentSyntax syntax)
		{
			var symbol = this.semanticModel.GetSpeculativeSymbolInfo(syntax.Expression.SpanStart + this.displacement, syntax.Expression, SpeculativeBindingOption.BindAsExpression).Symbol;

			var property = symbol?.GetType().GetProperty("Type");

			if (property != null)
			{
				return property.GetValue(symbol) as ITypeSymbol;
			}
			
			return this.semanticModel.GetSpeculativeTypeInfo(syntax.Expression.SpanStart + this.displacement, syntax.Expression, SpeculativeBindingOption.BindAsExpression).Type;
		}

		public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			this.lambdaStack.Push(node);

			try
			{
				return base.VisitAnonymousMethodExpression(node);
			}
			finally
			{
				this.lambdaStack.Pop();
			}
		}

		public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			this.lambdaStack.Push(node);

			try
			{
				return base.VisitParenthesizedLambdaExpression(node);
			}
			finally
			{
				this.lambdaStack.Pop();
			}
		}

		public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			this.lambdaStack.Push(node);

			try
			{
				return base.VisitSimpleLambdaExpression(node);
			}
			finally
			{
				this.lambdaStack.Pop();
			}
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
			
			var originalNodeStart = node.SpanStart + this.displacement;

			var explicitExtensionMethodCall = false;
			var result = this.semanticModel.GetSpeculativeSymbolInfo(node.SpanStart + displacement, node, SpeculativeBindingOption.BindAsExpression);

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
					&& (syncMethod = (IMethodSymbol)(this.semanticModel.ParentModel ?? this.semanticModel).GetSpeculativeSymbolInfo(node.SpanStart, newNode, SpeculativeBindingOption.BindAsExpression).Symbol) != null)
				{
					if (syncMethod.HasRewriteAsyncApplied())
					{
						var retval = node
							.WithExpression((ExpressionSyntax)visited)
							.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));

						var defaultExpression = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName($"Task<" + syncMethod.ReturnType + ">"));
						var model = this.semanticModel.ParentModel ?? this.semanticModel;

						var actualNode = this.semanticModel.SyntaxTree.GetRoot().FindNode(new TextSpan(originalNodeStart, node.Span.Length));

						this.displacement += -this.methodSyntax.SpanStart - (node.FullSpan.Length - defaultExpression.FullSpan.Length);
						this.methodSyntax = this.methodSyntax.ReplaceNode(actualNode, defaultExpression);
						this.displacement += this.methodSyntax.SpanStart;
						
						if (!model.TryGetSpeculativeSemanticModelForMethodBody(node.SpanStart, this.methodSyntax, out this.semanticModel))
						{
							this.log.LogError("Unable to GetSpeculativeSemanticModelForMethodBody");
						}

						return retval;
					}
				}

				return node
					.WithExpression((ExpressionSyntax)this.Visit(node.Expression))
					.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));
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
					return node
						.WithExpression((ExpressionSyntax)this.Visit(node.Expression))
						.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));
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
					var asyncCandidates2 = this.extensionMethodLookup.GetExtensionMethods(methodSymbol.Name + "Async", this.GetInvocationTargetType(originalNodeStart, node, methodSymbol)).ToList();

					candidate = asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(expectedParameterTypes, TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default))
						?? asyncCandidates2.FirstOrDefault(c => c.ExtensionMethodNormalizingParameters().Select(d => d.Type).SequenceEqual(methodParameters.Select(e => e.Type), TypeSymbolExtensions.EqualsToIgnoreGenericParametersEqualityComparer.Default));

					if (candidate != null)
					{
						explicitExtensionMethodCall = true;
					}
					else
					{
						return node
							.WithExpression((ExpressionSyntax)this.Visit(node.Expression))
							.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));
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

			node = node
				.WithExpression((ExpressionSyntax)this.Visit(node.Expression))
				.WithArgumentList((ArgumentListSyntax)this.VisitArgumentList(node.ArgumentList));

			return this.InspectExpression(node, cancellationTokenPos, candidate, explicitExtensionMethodCall);
		}

		private ITypeSymbol GetInvocationTargetType(int pos, InvocationExpressionSyntax node, IMethodSymbol methodSymbol)
		{
			ITypeSymbol retval;
			var notError = false;

			if (node.Expression is MemberAccessExpressionSyntax)
			{
				retval = this.semanticModel.GetSpeculativeTypeInfo(pos, ((MemberAccessExpressionSyntax)node.Expression).Expression, SpeculativeBindingOption.BindAsExpression).Type;
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
					retval = this.semanticModel.GetSpeculativeTypeInfo(pos, ((ConditionalAccessExpressionSyntax)node.Parent).Expression, SpeculativeBindingOption.BindAsExpression).Type;
				}
				else if (node.Parent is MemberAccessExpressionSyntax)
				{
					retval = this.semanticModel.GetSpeculativeTypeInfo(pos, ((MemberAccessExpressionSyntax)node.Parent).Expression, SpeculativeBindingOption.BindAsExpression).Type;
				}
				else
				{
					var property = node.Parent?.GetType().GetProperty("Expression");

					if (property != null)
					{
						retval =
							this.semanticModel.GetSpeculativeTypeInfo
								(pos, (SyntaxNode)property.GetValue(node.Parent), SpeculativeBindingOption.BindAsExpression).Type;
					}
					else
					{
						retval = null;
					}
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
					this.log.LogError($"Unable to determine type of {node.Expression} in {node.SyntaxTree.FilePath} {node.Expression.GetType()} at {node.GetLocation().GetMappedLineSpan()}");
				}
			}

			return retval;
		}

		protected abstract ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall);
	}
}
 