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
		private readonly HashSet<string> typesAlreadyWarnedAbout = new HashSet<string>();

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
				.AddRange(namespaces.SelectMany(c => GetNamespacesAndParents(c.Name.ToString()).Select(d => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(d)))))
				.Sort();

			usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true))));
			
			return compilationUnit.WithUsings(usings);
		}

		public string RewriteAndMerge(string[] paths, string[] additionalAssemblyNames = null, string[] excludeTypes = null)
		{
			var syntaxTrees = paths
				.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p)))
				.Select(c => c.WithRootAndOptions(UpdateUsings(c.GetCompilationUnitRoot()), c.Options))
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
				).NormalizeWhitespace("\t", "\r\n", false)
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

				if (!(syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
					.Any(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().StartsWith("RewriteAsync")))
					|| syntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
					.Any(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().StartsWith("RewriteAsync")))))
				{
					continue;
				}
				
				var namespaces = SyntaxFactory.List<MemberDeclarationSyntax>
				(
					syntaxTree.GetRoot()
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>()
					.Where(m => (m.Parent as TypeDeclarationSyntax)?.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().StartsWith("RewriteAsync"))  == true || m.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().StartsWith("RewriteAsync")))
					.Where(c => (c.FirstAncestorOrSelf<ClassDeclarationSyntax>() as TypeDeclarationSyntax ?? c.FirstAncestorOrSelf<InterfaceDeclarationSyntax>() as TypeDeclarationSyntax) != null)
					.GroupBy(m => m.FirstAncestorOrSelf<ClassDeclarationSyntax>() as TypeDeclarationSyntax ?? m.FirstAncestorOrSelf<InterfaceDeclarationSyntax>())
					.GroupBy(g => g.Key.FirstAncestorOrSelf<NamespaceDeclarationSyntax>())
					.Select(namespaceGrouping =>
						SyntaxFactory.NamespaceDeclaration(namespaceGrouping.Key.Name)
						.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>
						(
							namespaceGrouping.Select
							(
								typeGrouping => 
								typeGrouping.Key is ClassDeclarationSyntax
								?
									SyntaxFactory.ClassDeclaration(typeGrouping.Key.Identifier).WithModifiers(typeGrouping.Key.Modifiers)
									.WithTypeParameterList(typeGrouping.Key.TypeParameterList)
									.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(typeGrouping.SelectMany(m => this.RewriteMethods(m, semanticModel))))
									as TypeDeclarationSyntax
								:
									SyntaxFactory.InterfaceDeclaration(typeGrouping.Key.Identifier).WithModifiers(typeGrouping.Key.Modifiers)
									.WithTypeParameterList(typeGrouping.Key.TypeParameterList)
									.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(typeGrouping.SelectMany(m => this.RewriteMethods(m, semanticModel))))
									as TypeDeclarationSyntax
							)
						))
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
			yield return this.RewriteMethodAsync(inMethodSyntax, semanticModel, false);
			yield return this.RewriteMethodAsync(inMethodSyntax, semanticModel, true);
		}


		private IEnumerable<IMethodSymbol> GetMethods(SemanticModel semanticModel, ITypeSymbol symbol, string name, MethodDeclarationSyntax method)
		{
			var parameters = method.ParameterList.Parameters;

			return this
				.GetAllMembers(symbol)
				.Where(c => c.Name == name)
				.OfType<IMethodSymbol>()
				.Where(c => c.Parameters.Select(d => GetObj(d.Type)).SequenceEqual(parameters.Select(d => GetObj(GetSymbol(semanticModel, method, d.Type), method, d.Type))));
		}

		private object GetObj(ITypeSymbol symbol, MethodDeclarationSyntax method = null, TypeSyntax typeSyntax = null)
		{
			var typedParameterSymbol = symbol as ITypeParameterSymbol;

			if (typedParameterSymbol != null)
			{
				return typedParameterSymbol.Ordinal;
			}

			if (symbol != null)
			{
				return symbol;
			}

			return method.TypeParameterList.Parameters.IndexOf(c => c.ToString() == typeSyntax.ToString());
		}

		private INamedTypeSymbol GetSymbol(SemanticModel semanticModel, MethodDeclarationSyntax method, TypeSyntax typeSyntax)
		{
			var position = method.SpanStart;
			
			var retval = semanticModel.GetSpeculativeSymbolInfo(position, typeSyntax, SpeculativeBindingOption.BindAsExpression).Symbol as INamedTypeSymbol;

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
		
		private MethodDeclarationSyntax RewriteMethodAsync(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, bool cancellationVersion)
		{
			var methodSymbol = (IMethodSymbol)ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax);
			var asyncMethodName = methodSymbol.Name + "Async";
			var isInterfaceMethod = methodSymbol.ContainingType.TypeKind == TypeKind.Interface;

			if (((methodSyntax.Parent as TypeDeclarationSyntax)?.Modifiers)?.Any(c => c.Kind() == SyntaxKind.PartialKeyword) != true)
			{
				var name = ((TypeDeclarationSyntax)methodSyntax.Parent).Identifier.ToString();

				if (!typesAlreadyWarnedAbout.Contains(name))
				{
					typesAlreadyWarnedAbout.Add(name);

					Console.WriteLine($"Type '{name}' needs to be marked as partial");
				}
			}

			var rewriter = new MethodInvocationRewriter(this.log, semanticModel, this.excludedTypes, this.cancellationTokenSymbol);
			var newAsyncMethod = (MethodDeclarationSyntax)rewriter.Visit(methodSyntax);
			var returnTypeName = methodSyntax.ReturnType.ToString();

			newAsyncMethod = newAsyncMethod
				.WithIdentifier(SyntaxFactory.Identifier(asyncMethodName))
				.WithAttributeLists(new SyntaxList<AttributeListSyntax>())
				.WithReturnType(SyntaxFactory.ParseTypeName(returnTypeName == "void" ? "Task" : $"Task<{returnTypeName}>"));

			if (cancellationVersion)
			{
				newAsyncMethod = newAsyncMethod.WithParameterList(SyntaxFactory.ParameterList(methodSyntax.ParameterList.Parameters.Insert
				(
					methodSyntax.ParameterList.Parameters.TakeWhile(p => p.Default == null && !p.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword))).Count(),
					SyntaxFactory.Parameter
					(
						SyntaxFactory.List<AttributeListSyntax>(),
						SyntaxFactory.TokenList(),
						SyntaxFactory.ParseTypeName(this.cancellationTokenSymbol.ToMinimalDisplayString(semanticModel, newAsyncMethod.SpanStart)),
						SyntaxFactory.Identifier("cancellationToken"),
						null
					)
				)));

				if (!isInterfaceMethod)
				{
					newAsyncMethod = newAsyncMethod.WithModifiers(methodSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)));
				}
			}
			else
			{
				var callAsyncWithCancellationToken = SyntaxFactory.InvocationExpression
				(
					SyntaxFactory.IdentifierName(asyncMethodName),
					SyntaxFactory.ArgumentList
					(
						new SeparatedSyntaxList<ArgumentSyntax>()
						.AddRange(methodSymbol.Parameters.TakeWhile(c => !c.HasExplicitDefaultValue).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
						.Add(SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName("CancellationToken"), SyntaxFactory.IdentifierName("None"))))
						.AddRange(methodSymbol.Parameters.SkipWhile(c => !c.HasExplicitDefaultValue).Skip(1).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
					)
				);

				if (!isInterfaceMethod)
				{
					newAsyncMethod = newAsyncMethod.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(callAsyncWithCancellationToken)));
				}
			}

			if (!isInterfaceMethod)
			{
				var baseAsyncMethod = this
					.GetMethods(semanticModel, methodSymbol.ReceiverType.BaseType, methodSymbol.Name + "Async", newAsyncMethod)
					.FirstOrDefault();

				var baseMethod = this
					.GetMethods(semanticModel, methodSymbol.ReceiverType.BaseType, methodSymbol.Name, methodSyntax)
					.FirstOrDefault();

				var parentContainsAsyncMethod = baseAsyncMethod != null;
				var parentContainsMethodWithRewriteAsync = 
					baseMethod?.GetAttributes().Any(c => c.AttributeClass.Name.StartsWith("RewriteAsync")) == true
					|| baseMethod?.ContainingType.GetAttributes().Any(c => c.AttributeClass.Name.StartsWith("RewriteAsync")) == true;
				var hadNew = newAsyncMethod.Modifiers.Any(c => c.Kind() == SyntaxKind.NewKeyword);
				var hadOverride = newAsyncMethod.Modifiers.Any(c => c.Kind() == SyntaxKind.OverrideKeyword);

				if (!parentContainsAsyncMethod && hadNew)
				{
					newAsyncMethod = newAsyncMethod.WithModifiers(new SyntaxTokenList().AddRange(newAsyncMethod.Modifiers.Where(c => c.Kind() != SyntaxKind.NewKeyword)));
				}

				if (!(parentContainsAsyncMethod || parentContainsMethodWithRewriteAsync))
				{
					newAsyncMethod = newAsyncMethod.WithModifiers(new SyntaxTokenList().AddRange(newAsyncMethod.Modifiers.Where(c => c.Kind() != SyntaxKind.OverrideKeyword)));

					if (hadOverride)
					{
						newAsyncMethod = newAsyncMethod.WithModifiers(newAsyncMethod.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword)));
					}
				}

				var baseMatchedMethod = baseMethod ?? baseAsyncMethod;

				if (methodSyntax.ConstraintClauses.Any())
				{
					newAsyncMethod = newAsyncMethod.WithConstraintClauses(methodSyntax.ConstraintClauses);
				}
				else if (!hadOverride && baseMatchedMethod != null)
				{
					var constraintClauses = new List<TypeParameterConstraintClauseSyntax>();

					foreach (var typeParameter in baseMatchedMethod.TypeParameters)
					{
						var constraintClause = SyntaxFactory.TypeParameterConstraintClause(typeParameter.Name);
						var constraints = new List<TypeParameterConstraintSyntax>();

						if (typeParameter.HasReferenceTypeConstraint)
						{
							constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint));
						}

						if (typeParameter.HasValueTypeConstraint)
						{
							constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint));
						}

						if (typeParameter.HasConstructorConstraint)
						{
							constraints.Add(SyntaxFactory.ConstructorConstraint());
						}

						constraints.AddRange(typeParameter.ConstraintTypes.Select(c => SyntaxFactory.TypeConstraint(SyntaxFactory.ParseName(c.ToMinimalDisplayString(semanticModel, methodSyntax.SpanStart)))));

						constraintClause = constraintClause.WithConstraints(SyntaxFactory.SeparatedList(constraints));
						constraintClauses.Add(constraintClause);
					}

					newAsyncMethod = newAsyncMethod.WithConstraintClauses(SyntaxFactory.List(constraintClauses));
				}
			}

			var attribute = methodSymbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"))
							?? methodSymbol.ContainingType.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name.EndsWith("RewriteAsyncAttribute"));

			if (attribute?.ConstructorArguments.Length > 0)
			{
				var first = attribute.ConstructorArguments.First();

				if (first.Type.Equals(this.methodAttributesSymbol))
				{
					var methodAttributes = (MethodAttributes)Enum.ToObject(typeof(MethodAttributes), Convert.ToInt32(first.Value));

					newAsyncMethod = newAsyncMethod.WithAccessModifiers(methodAttributes);
				}
			}

			return newAsyncMethod;
		}
	}
}
