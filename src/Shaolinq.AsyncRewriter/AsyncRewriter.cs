using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public class Rewriter
	{
		private static readonly string[] alwaysExcludedTypeNames =
		{
			"System.IO.TextWriter",
			"System.IO.StringWriter",
			"System.IO.MemoryStream"
		};

		private HashSet<ITypeSymbol> excludedTypes;
		private ITypeSymbol cancellationTokenSymbol;

		private static readonly UsingDirectiveSyntax[] extraUsingDirectives =
		{
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading")),
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")),
		};

		private readonly ILogger log;

		public Rewriter(ILogger log = null)
		{
			this.log = log;
		}

		public string RewriteAndMerge(string[] paths, string[] additionalAssemblyNames = null, string[] excludeTypes = null)
		{
			var syntaxTrees = paths.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p))).ToArray();

			var compilation = CSharpCompilation
				.Create("Temp", syntaxTrees)
				.AddReferences
				(
					MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Stream).GetTypeInfo().Assembly.Location)
				);

			if (additionalAssemblyNames != null)
			{
				var assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

				if (assemblyPath == null)
				{
					throw new InvalidOperationException();
				}

				compilation = compilation.AddReferences(additionalAssemblyNames.Select(n =>
				{
					if (File.Exists(n))
					{
						return MetadataReference.CreateFromFile(n);
					}

					if (File.Exists(Path.Combine(assemblyPath, n)))
					{
						return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, n));
					}

					return null;
				}).Where(c => c != null));
			}

			return this.RewriteAndMerge(syntaxTrees, compilation, excludeTypes).ToString();
		}

		private class UsingsComparer
			: IEqualityComparer<UsingDirectiveSyntax>
		{
			public static readonly UsingsComparer Default = new UsingsComparer();

			private UsingsComparer()
			{
			}

			public bool Equals(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
			{
				return x.Name.ToString() == y.Name.ToString();
			}

			public int GetHashCode(UsingDirectiveSyntax obj)
			{
				return obj.Name.ToString().GetHashCode();
			}
		}

		public SyntaxTree RewriteAndMerge(SyntaxTree[] syntaxTrees, CSharpCompilation compilation, string[] excludeTypes = null)
		{
			var rewrittenTrees = this.Rewrite(syntaxTrees, compilation, excludeTypes).ToArray();

			return SyntaxFactory.SyntaxTree
			(
				SyntaxFactory.CompilationUnit()
					.WithUsings(SyntaxFactory.List(new HashSet<UsingDirectiveSyntax>(rewrittenTrees.SelectMany(t => t.GetCompilationUnitRoot().Usings), UsingsComparer.Default)))
					.WithMembers
					(
						SyntaxFactory.List<MemberDeclarationSyntax>
						(rewrittenTrees
							.SelectMany(t => t.GetCompilationUnitRoot().Members)
							.Cast<NamespaceDeclarationSyntax>()
							.SelectMany(ns => ns.Members)
							.Cast<ClassDeclarationSyntax>()
							.GroupBy(cls => cls.FirstAncestorOrSelf<NamespaceDeclarationSyntax>().Name.ToString())
							.Select(g => SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(g.Key)).WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(g)))
						)
					)
					.WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
					.NormalizeWhitespace()
			);
		}

		public IEnumerable<SyntaxTree> Rewrite(SyntaxTree[] syntaxTrees, CSharpCompilation compilation, string[] excludeTypes = null)
		{
			this.cancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

			this.excludedTypes = new HashSet<ITypeSymbol>();

			if (excludeTypes != null)
			{
				var excludedTypeSymbols = excludeTypes.Select(compilation.GetTypeByMetadataName).ToList();
				var notFound = excludedTypeSymbols.IndexOf(null);

				if (notFound != -1)
				{
					throw new ArgumentException($"Type {excludeTypes[notFound]} not found in compilation", nameof(excludeTypes));
				}

				this.excludedTypes.UnionWith(excludedTypeSymbols);
			}

			this.excludedTypes.UnionWith(alwaysExcludedTypeNames.Select(compilation.GetTypeByMetadataName).Where(sym => sym != null));

			foreach (var syntaxTree in syntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

				if (semanticModel == null)
				{
					throw new ArgumentException("A provided syntax tree was compiled into the provided compilation");
				}

				var usings = syntaxTree.GetCompilationUnitRoot().Usings;

				if (!syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().Contains("RewriteAsync"))))
				{
					continue;
				}

				usings = usings.AddRange(extraUsingDirectives);
				usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true))));

				var namespaces = SyntaxFactory.List<MemberDeclarationSyntax>(
					syntaxTree.GetRoot()
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>()
					.Where(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().Contains("RewriteAsync")))
					.GroupBy(m => m.FirstAncestorOrSelf<ClassDeclarationSyntax>())
					.GroupBy(g => g.Key.FirstAncestorOrSelf<NamespaceDeclarationSyntax>())
					.Select(nsGrp =>
						SyntaxFactory.NamespaceDeclaration(nsGrp.Key.Name)
						.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(nsGrp.Select(clsGrp =>
							SyntaxFactory.ClassDeclaration(clsGrp.Key.Identifier)
								.WithModifiers(clsGrp.Key.Modifiers)
								.WithTypeParameterList(clsGrp.Key.TypeParameterList)
								.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(clsGrp.SelectMany(m => this.RewriteMethods(m, semanticModel))))
						)))
					)
				);

				yield return SyntaxFactory.SyntaxTree
				(
					SyntaxFactory.CompilationUnit()
						.WithUsings(SyntaxFactory.List(usings))
						.WithMembers(namespaces)
						.WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
						.NormalizeWhitespace()
				);
			}
		}

		IEnumerable<MethodDeclarationSyntax> RewriteMethods(MethodDeclarationSyntax inMethodSyntax, SemanticModel semanticModel)
		{
			yield return this.RewriteMethodAsync(inMethodSyntax, semanticModel);
			yield return this.RewriteMethodAsyncWithCancellationToken(inMethodSyntax, semanticModel);
		}
		
		MethodDeclarationSyntax RewriteMethodAsync(MethodDeclarationSyntax inMethodSyntax, SemanticModel semanticModel)
		{
			var inMethodSymbol = (IMethodSymbol)ModelExtensions.GetDeclaredSymbol(semanticModel, inMethodSyntax);

			var outMethodName = inMethodSyntax.Identifier.Text + "Async";

			var methodInvocation = SyntaxFactory.InvocationExpression
			(
				SyntaxFactory.IdentifierName(outMethodName),
				SyntaxFactory.ArgumentList
				(
					new SeparatedSyntaxList<ArgumentSyntax>()
					.AddRange(inMethodSymbol.Parameters.TakeWhile(c => !c.HasExplicitDefaultValue).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
					.Add(SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("CancellationToken"), SyntaxFactory.IdentifierName("None"))))
					.AddRange(inMethodSymbol.Parameters.SkipWhile(c => !c.HasExplicitDefaultValue).Skip(1).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
				)
			);

			var callAsyncWithCancellationToken = methodInvocation;

			var returnType = inMethodSyntax.ReturnType.ToString();
			var method = inMethodSyntax.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(callAsyncWithCancellationToken)));
			
			method = method
				.WithIdentifier(SyntaxFactory.Identifier(outMethodName))
				.WithAttributeLists(new SyntaxList<AttributeListSyntax>())
				.WithReturnType(SyntaxFactory.ParseTypeName(returnType == "void" ? "Task" : $"Task<{returnType}>"));

			var parentContainsAsyncMethod = this.GetAllMembers(inMethodSymbol.ReceiverType.BaseType).Any(c => c.Name == outMethodName);
			var parentContainsMethodWithRewriteAsync = this.GetAllMembers(inMethodSymbol.ReceiverType.BaseType)
				.Where(c => c.Name == inMethodSyntax.Identifier.Text)
				.Any(m => m.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")));

			if (!(parentContainsAsyncMethod || parentContainsMethodWithRewriteAsync))
			{
				method = method.WithModifiers(new SyntaxTokenList().AddRange(method.Modifiers.Where(c => c.Kind() != SyntaxKind.OverrideKeyword && c.Kind() != SyntaxKind.NewKeyword)));
			}

			var attribute = inMethodSymbol.GetAttributes().Single(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"));

			if (attribute.ConstructorArguments.Any())
			{
				if (attribute.ConstructorArguments.First().Type.Name == "MethodAttributes")
				{
					var methodAttributes = (MethodAttributes)Enum.ToObject(typeof(MethodAttributes), Convert.ToInt32(attribute.ConstructorArguments.First().Value));

					method = method.WithAccessModifiers(methodAttributes);
				}
			}

			return method;
		}

		private IEnumerable<ISymbol> GetAllMembers(ITypeSymbol symbol)
		{
			foreach (var member in symbol.GetMembers())
			{
				yield return member;
			}

			if (symbol.BaseType != null)
			{
				foreach (var member in symbol.BaseType.GetMembers())
				{
					yield return member;
				}
			}
		}

		private MethodDeclarationSyntax RewriteMethodAsyncWithCancellationToken(MethodDeclarationSyntax inMethodSyntax, SemanticModel semanticModel)
		{
			var inMethodSymbol = (IMethodSymbol)ModelExtensions.GetDeclaredSymbol(semanticModel, inMethodSyntax);

			var outMethodName = inMethodSyntax.Identifier.Text + "Async";

			var rewriter = new MethodInvocationRewriter(this.log, semanticModel, this.excludedTypes, this.cancellationTokenSymbol);
			var method = (MethodDeclarationSyntax)rewriter.Visit(inMethodSyntax);
			
			method = method
				.WithIdentifier(SyntaxFactory.Identifier(outMethodName))
				.WithAttributeLists(new SyntaxList<AttributeListSyntax>())
				.WithModifiers(inMethodSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))
				.WithParameterList(SyntaxFactory.ParameterList(inMethodSyntax.ParameterList.Parameters.Insert
				(
					inMethodSyntax.ParameterList.Parameters.TakeWhile(p => p.Default == null && !p.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword))).Count(),
					SyntaxFactory.Parameter
					(
						SyntaxFactory.List<AttributeListSyntax>(),
						SyntaxFactory.TokenList(),
						SyntaxFactory.ParseTypeName("CancellationToken"),
						SyntaxFactory.Identifier("cancellationToken"),
						null
					)
				)));
			
			var returnType = inMethodSyntax.ReturnType.ToString();

			method = method.WithReturnType(SyntaxFactory.ParseTypeName(returnType == "void" ? "Task" : $"Task<{returnType}>"));

			var parentContainsAsyncMethod = this.GetAllMembers(inMethodSymbol.ReceiverType.BaseType).Any(c => c.Name == outMethodName);
			var parentContainsMethodWithRewriteAsync = this.GetAllMembers(inMethodSymbol.ReceiverType.BaseType)
				.Where(c => c.Name == inMethodSyntax.Identifier.Text)
				.Any(m => m.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")));

			if (!(parentContainsAsyncMethod || parentContainsMethodWithRewriteAsync))
			{
				method = method.WithModifiers(new SyntaxTokenList().AddRange(method.Modifiers.Where(c => c.Kind() != SyntaxKind.OverrideKeyword && c.Kind() != SyntaxKind.NewKeyword)));
			}

			var attribute = inMethodSymbol.GetAttributes().Single(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"));

			if (attribute.ConstructorArguments.Length > 0)
			{
				if (attribute.ConstructorArguments[0].Type.Name == "MethodAttributes")
				{
					var methodAttributes = (MethodAttributes)Enum.ToObject(typeof(MethodAttributes), Convert.ToInt32(attribute.ConstructorArguments[0].Value));

					method = method.WithAccessModifiers(methodAttributes);
				}
			}

			return method;
		}
	}
	
	internal class ParameterComparer : IEqualityComparer<IParameterSymbol>
	{
		public static readonly ParameterComparer Default = new ParameterComparer();

		public bool Equals(IParameterSymbol x, IParameterSymbol y)
		{
			return x.Name.Equals(y.Name) && x.Type.Equals(y.Type);
		}

		public int GetHashCode(IParameterSymbol p)
		{
			return p.GetHashCode();
		}
	}

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
