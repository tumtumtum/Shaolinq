using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Platform;

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
		private ITypeSymbol methodAttributesSymbol;
		private ITypeSymbol cancellationTokenSymbol;

		private static readonly UsingDirectiveSyntax[] extraUsingDirectives =
		{
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading")),
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"))
		};

		private readonly ILogger log;
		

		public Rewriter(ILogger log = null)
		{
			this.log = log;
		}

		private static IEnumerable<string> GetNamespacesAndParents(string nameSpace)
		{
			var parts = nameSpace.Split('.');

			var current = new StringBuilder();

			foreach (var part in parts)
			{
				current.Append(part);

				yield return current.ToString();

				current.Append('.');
			}
		}

		private CompilationUnitSyntax UpdateUsings(CompilationUnitSyntax compilationUnit)
		{
			var namespaces = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().ToList();
			var usings = compilationUnit.Usings;
			
			usings = usings
				.AddRange(extraUsingDirectives)
				.AddRange(namespaces.SelectMany(c => GetNamespacesAndParents(c.Name.ToString()).Select(d => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(d)))));

			usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true))));
			
			return compilationUnit.WithUsings(usings);
		}

		public string RewriteAndMerge(string[] paths, string[] additionalAssemblyNames = null, string[] excludeTypes = null)
		{
			var syntaxTrees = paths
				.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p)))
				.Select(c => c.WithRootAndOptions(UpdateUsings(c.GetCompilationUnitRoot()).NormalizeWhitespace(), c.Options))
				.ToArray();
			
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

		private static UsingDirectiveSyntax CreateUsingDirective(string usingName)
		{
			NameSyntax qualifiedName = null;

			foreach (var identifier in usingName.Split('.'))
			{
				var name = SyntaxFactory.IdentifierName(identifier);

				if (qualifiedName != null)
				{
					qualifiedName = SyntaxFactory.QualifiedName(qualifiedName, name);
				}
				else
				{
					qualifiedName = name;
				}
			}

			return SyntaxFactory.UsingDirective(qualifiedName);
		}
		
		private static NamespaceDeclarationSyntax AmendUsings(NamespaceDeclarationSyntax nameSpace, SyntaxList<UsingDirectiveSyntax> usings)
		{
			var last = nameSpace.Name.ToString().Right(c => c != '.');
			
			foreach (var item in usings)
			{
				var name = item.Name.ToString();
				var first = name.Left(c => c != '.');

				if (first == last)
				{
					usings = usings.Remove(item);
					usings = usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("global::" + name)));
				}
			}

			return nameSpace.WithUsings(usings);
		}

		private SyntaxTree RewriteAndMerge(SyntaxTree[] syntaxTrees, CSharpCompilation compilation, string[] excludeTypes = null)
		{
			var rewrittenTrees = this.Rewrite(syntaxTrees, compilation, excludeTypes).ToArray();
			
			return SyntaxFactory.SyntaxTree
			(
				SyntaxFactory.CompilationUnit()
				.WithMembers
				(
					SyntaxFactory.List
					(
						rewrittenTrees.SelectMany
						(
							c => c.GetCompilationUnitRoot().Members
						)
					)
				)
				.NormalizeWhitespace()
			);
		}

		public IEnumerable<SyntaxTree> Rewrite(SyntaxTree[] syntaxTrees, CSharpCompilation compilation, string[] excludeTypes = null)
		{
			this.methodAttributesSymbol = compilation.GetTypeByMetadataName(typeof(MethodAttributes).FullName);
			this.cancellationTokenSymbol = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName);
			
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

				if (!syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().Contains("RewriteAsync"))))
				{
					continue;
				}
				
				var namespaces = SyntaxFactory.List<MemberDeclarationSyntax>
				(
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
						.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(namespaces.OfType<NamespaceDeclarationSyntax>().Select(c => AmendUsings(c, syntaxTree.GetCompilationUnitRoot().Usings))))
						.WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
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

			var parentContainsAsyncMethod = this
				.GetMethods(semanticModel, inMethodSymbol.ReceiverType.BaseType, outMethodName, method)
				.Any();
				
			var parentContainsMethodWithRewriteAsync = this
				.GetMethods(semanticModel, inMethodSymbol.ReceiverType.BaseType, inMethodSyntax.Identifier.Text, inMethodSyntax)
				.Any(m => m.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")));
			
			if (!(parentContainsAsyncMethod || parentContainsMethodWithRewriteAsync))
			{
				var hadOverride = method.Modifiers.Any(c => c.Kind() == SyntaxKind.OverrideKeyword);

				method = method.WithModifiers(new SyntaxTokenList().AddRange(method.Modifiers.Where(c => c.Kind() != SyntaxKind.OverrideKeyword && c.Kind() != SyntaxKind.NewKeyword)));

				if (hadOverride)
				{
					method = method.WithModifiers(method.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword)));
				}
			}

			var attribute = inMethodSymbol
				.GetAttributes()
				.SingleOrDefault(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"));

			if (attribute?.ConstructorArguments.Any() == true)
			{
				if (attribute.ConstructorArguments.First().Type == methodAttributesSymbol)
				{
					var methodAttributes = (MethodAttributes)Enum.ToObject(typeof(MethodAttributes), Convert.ToInt32(attribute.ConstructorArguments.First().Value));

					method = method.WithAccessModifiers(methodAttributes);
				}
			}

			return method;
		}

		private IEnumerable<IMethodSymbol> GetMethods(SemanticModel semanticModel, ITypeSymbol symbol, string name, MethodDeclarationSyntax method)
		{
			var parameters = method.ParameterList.Parameters;

			return this
				.GetAllMembers(symbol)
				.Where(c => c.Name == name)
				.OfType<IMethodSymbol>()
				.Where(c => c.Parameters.Select(d => d.Type).SequenceEqual(parameters.Select(d => GetSymbol(semanticModel, method, d.Type))));
		}

		private INamedTypeSymbol GetSymbol(SemanticModel semanticModel, MethodDeclarationSyntax method, TypeSyntax typeSyntax)
		{
			var position = method.SpanStart;
			
			var retval = semanticModel.GetSpeculativeSymbolInfo(position, typeSyntax, SpeculativeBindingOption.BindAsExpression).Symbol as INamedTypeSymbol;
			
			if (retval == null)
			{
				Console.WriteLine($"Unable to find symbol for {typeSyntax}");
			}

			return retval;
		}

		private IEnumerable<ISymbol> GetAllMembers(ITypeSymbol symbol, bool includeParents = true)
		{
			foreach (var member in symbol.GetMembers())
			{
				yield return member;
			}

			if (symbol.BaseType != null && includeParents)
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
						SyntaxFactory.ParseTypeName(this.cancellationTokenSymbol.ToMinimalDisplayString(semanticModel, method.SpanStart)),
						SyntaxFactory.Identifier("cancellationToken"),
						null
					)
				)));

			var returnType = inMethodSyntax.ReturnType.ToString();

			method = method.WithReturnType(SyntaxFactory.ParseTypeName(returnType == "void" ? "Task" : $"Task<{returnType}>"));

			var parentContainsAsyncMethod = this
				.GetMethods(semanticModel, inMethodSymbol.ReceiverType.BaseType, outMethodName, method)
				.Any();
				
			var parentContainsMethodWithRewriteAsync = this
				.GetMethods(semanticModel, inMethodSymbol.ReceiverType.BaseType, inMethodSyntax.Identifier.Text, inMethodSyntax)
				.Any(m => m.GetAttributes().Any(a => a.AttributeClass.Name.Contains("RewriteAsync")));
			
			if (!(parentContainsAsyncMethod || parentContainsMethodWithRewriteAsync))
			{
				var hadOverride = method.Modifiers.Any(c => c.Kind() == SyntaxKind.OverrideKeyword);

				method = method.WithModifiers(new SyntaxTokenList().AddRange(method.Modifiers.Where(c => c.Kind() != SyntaxKind.OverrideKeyword && c.Kind() != SyntaxKind.NewKeyword)));

				if (hadOverride)
				{
					method = method.WithModifiers(method.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword)));
				}
			}

			var attribute = inMethodSymbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"));

			if (attribute?.ConstructorArguments.Length > 0)
			{
				if (attribute.ConstructorArguments[0].Type == this.methodAttributesSymbol)
				{
					var methodAttributes = (MethodAttributes)Enum.ToObject(typeof(MethodAttributes), Convert.ToInt32(attribute.ConstructorArguments[0].Value));

					method = method.WithAccessModifiers(methodAttributes);
				}
			}

			return method;
		}
	}
}
