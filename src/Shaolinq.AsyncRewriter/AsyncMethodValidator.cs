// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class AsyncMethodValidator : MethodInvocationAsyncRewriter
	{
		public struct ValidatorResult
		{
			public string FileName => this.MethodInvocationSyntax.SyntaxTree.FilePath;
			public Location Position => this.MethodInvocationSyntax.GetLocation();
			public InvocationExpressionSyntax MethodInvocationSyntax { get; set; }
			public ExpressionSyntax ReplacementExpressionSyntax { get; set; }
			public IMethodSymbol ReplacementMethodSymbol { get; set; }
		}

		private readonly List<ValidatorResult> results = new List<ValidatorResult>();

		public static List<ValidatorResult> Validate(MethodDeclarationSyntax methodSyntax, IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol)
		{
			var validator = new AsyncMethodValidator(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax);

			validator.Visit(methodSyntax);

			return validator.results;
		}

		private AsyncMethodValidator(IAsyncRewriterLogger log, CompilationLookup extensionMethodLookup, SemanticModel semanticModel, HashSet<ITypeSymbol> excludeTypes, ITypeSymbol cancellationTokenSymbol, MethodDeclarationSyntax methodSyntax)
			: base(log, extensionMethodLookup, semanticModel, excludeTypes, cancellationTokenSymbol, methodSyntax)
		{
		}

		protected override ExpressionSyntax InspectExpression(InvocationExpressionSyntax node, int cancellationTokenPos, IMethodSymbol candidate, bool explicitExtensionMethodCall, int candidateCount)
		{
			var result = base.InspectExpression(node, cancellationTokenPos, candidate, explicitExtensionMethodCall, candidateCount);

			this.results.Add(new ValidatorResult { MethodInvocationSyntax = node, ReplacementExpressionSyntax = result, ReplacementMethodSymbol = candidate });

			return node;
		}
	}
}