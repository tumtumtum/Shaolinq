using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

		private CompilationLookup lookup;
		private readonly IAsyncRewriterLogger log;
		private HashSet<ITypeSymbol> excludedTypes;
		private ITypeSymbol methodAttributesSymbol;
		private ITypeSymbol cancellationTokenSymbol;
		private CSharpCompilation compilation;
		private readonly HashSet<string> typesAlreadyWarnedAbout = new HashSet<string>();
		
		private static readonly UsingDirectiveSyntax[] extraUsingDirectives =
		{
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading")),
			SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"))
		};

		public Rewriter(IAsyncRewriterLogger log = null)
		{
			this.log = log ?? TextAsyncRewriterLogger.ConsoleLogger;
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
				.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p)).WithFilePath(p))
				.Select(c => c.WithRootAndOptions(UpdateUsings(c.GetCompilationUnitRoot()), c.Options))
				.ToArray();

			var references = new List<MetadataReference>()
			{
				MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Queryable).GetTypeInfo().Assembly.Location)
			};
			
			if (additionalAssemblyNames != null)
			{
				var assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

				log.LogMessage("FrameworkPath: " + assemblyPath);

				if (assemblyPath == null)
				{
					throw new InvalidOperationException();
				}

				var facadesLoaded = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

				var loadedAssemblies = new List<string>();

				references.AddRange(additionalAssemblyNames.SelectMany(n =>
				{
					var results = new List<MetadataReference>();
					
					if (File.Exists(n))
					{
					   var facadesPath = Path.Combine(Path.GetDirectoryName(n) ?? "", "Facades");

						results.Add(MetadataReference.CreateFromFile(n));
						
						loadedAssemblies.Add(n);

						if (Directory.Exists(facadesPath) && !facadesLoaded.Contains(facadesPath))
						{
							facadesLoaded.Add(facadesPath);

							results.AddRange(Directory.GetFiles(facadesPath).Select(facadeDll => MetadataReference.CreateFromFile(facadeDll)));
						}

						return results;
					}

					if (File.Exists(Path.Combine(assemblyPath, n)))
					{
						results.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, n)));

						return results;
					}

					log.LogError($"Could find ResolveProjectReferencesResolveProjectReferencesreferenced assembly: {n}");

					return results;
				}).Where(c => c != null));

				log.LogMessage($"Loaded assemblies: {string.Join(",", loadedAssemblies)}");
			}

			var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

			this.compilation = CSharpCompilation
				.Create("Temp", syntaxTrees, references, options);

			this.lookup = new CompilationLookup(this.compilation);

			var retval = this.RewriteAndMerge(syntaxTrees, compilation, excludeTypes).ToString();

#if OUTPUT_COMPILER_ERRORS
			var emitResult = this.compilation.Emit(Stream.Null);

			if (emitResult.Diagnostics.Any())
			{
				log.LogMessage("Compiler errors:");
			
				foreach (var diagnostic in emitResult.Diagnostics)
				{
					var message = diagnostic.GetMessage();

					if (diagnostic.Severity == DiagnosticSeverity.Info)
					{
						log.LogError(message);
					}
					else
					{
						log.LogMessage(message);
					}
				}
			}
#endif

			return retval;
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

		private SyntaxTree RewriteAndMerge(SyntaxTree[] syntaxTrees, CSharpCompilation compilationNode, string[] excludeTypes = null)
		{
			var rewrittenTrees = this.Rewrite(syntaxTrees, compilationNode, excludeTypes).ToArray();
			
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
				).NormalizeWhitespace("\t")
			);
		}

		public IEnumerable<SyntaxTree> Rewrite(SyntaxTree[] syntaxTrees, CSharpCompilation compilationNode, string[] excludeTypes = null)
		{
			this.methodAttributesSymbol = compilationNode.GetTypeByMetadataName(typeof(MethodAttributes).FullName);
			this.cancellationTokenSymbol = compilationNode.GetTypeByMetadataName(typeof(CancellationToken).FullName);
			
			this.excludedTypes = new HashSet<ITypeSymbol>();

			if (excludeTypes != null)
			{
				var excludedTypeSymbols = excludeTypes.Select(compilationNode.GetTypeByMetadataName).ToList();
				var notFound = excludedTypeSymbols.IndexOf(null);

				if (notFound != -1)
				{
					throw new ArgumentException($"Type {excludeTypes[notFound]} not found in compilation", nameof(excludeTypes));
				}

				this.excludedTypes.UnionWith(excludedTypeSymbols);
			}

			this.excludedTypes.UnionWith(alwaysExcludedTypeNames.Select(compilationNode.GetTypeByMetadataName).Where(sym => sym != null));

			foreach (var syntaxTree in syntaxTrees)
			{
				var semanticModel = compilationNode.GetSemanticModel(syntaxTree, true);

				if (semanticModel == null)
				{
					throw new ArgumentException("A provided syntax tree was compiled into the provided compilation");
				}
				
				var asyncMethods = syntaxTree
					.GetRoot()
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>()
					.Where(m => m.Modifiers.Any(c => c.Kind() == SyntaxKind.AsyncKeyword));

				foreach (var asyncMethod in asyncMethods)
				{
					ValidateAsyncMethod(asyncMethod, semanticModel);
				}

				if (syntaxTree.GetRoot().DescendantNodes().All(m => ((m as MethodDeclarationSyntax)?.AttributeLists ?? (m as TypeDeclarationSyntax)?.AttributeLists)?.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().StartsWith("RewriteAsync")) == null))
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
			var result = this.RewriteMethodAsync(inMethodSyntax, semanticModel, false);

			if (result != null)
			{
				yield return result;
			}

			yield return this.RewriteMethodAsync(inMethodSyntax, semanticModel, true);
		}
		
		private IEnumerable<IMethodSymbol> GetMethods(SemanticModel semanticModel, ITypeSymbol symbol, string name, MethodDeclarationSyntax method, MethodDeclarationSyntax originalMethod)
		{
			var parameters = method.ParameterList.Parameters;

			var retval = this
				.GetAllMembers(symbol)
				.Where(c => c.Name == name)
				.OfType<IMethodSymbol>()
				.Where(c => c.Parameters.Select(d => this.GetParameterTypeComparisonKey(d.Type)).SequenceEqual(parameters.Select(d => this.GetParameterTypeComparisonKey(semanticModel.GetSpeculativeTypeInfo(originalMethod.ParameterList.SpanStart, d.Type, SpeculativeBindingOption.BindAsExpression).Type, method, d.Type))));

		    return retval;
		}

		private object GetParameterTypeComparisonKey(ITypeSymbol symbol, MethodDeclarationSyntax method = null, TypeSyntax typeSyntax = null)
		{
			var typedParameterSymbol = symbol as ITypeParameterSymbol;

			if (typedParameterSymbol != null)
			{
				return typedParameterSymbol.Ordinal;
			}

			if (symbol != null && symbol.TypeKind != TypeKind.Error)
			{
				return symbol;
			}

		    return method?.TypeParameterList?.Parameters.IndexOf(c => c?.Identifier.Text == typeSyntax?.ToString()) ?? (object)symbol;
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

		private void ValidateAsyncMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
		{
			var methodSymbol = (IMethodSymbol)ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax);
			var results = AsyncMethodValidator.Validate(methodSyntax, log, this.lookup, semanticModel, excludedTypes, cancellationTokenSymbol);

			if (results.Count > 0)
			{
				foreach (var result in results)
				{
					if (!result.ReplacementMethodSymbol.Equals(methodSymbol))
					{
						log.LogWarning($"Async method call possible in {result.Position.GetLineSpan()}");
						log.LogWarning($"Replace {result.MethodInvocationSyntax.NormalizeWhitespace()} with {result.ReplacementExpressionSyntax.NormalizeWhitespace()}");
					}
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

					log.LogError($"Type '{name}' needs to be marked as partial");
				}
			}

			var newAsyncMethod = MethodInvocationAsyncRewriter.Rewrite(this.log, this.lookup, semanticModel, this.excludedTypes, this.cancellationTokenSymbol, methodSyntax);
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

				if (!(isInterfaceMethod || methodSymbol.IsAbstract))
				{
					newAsyncMethod = newAsyncMethod.WithModifiers(methodSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)));
				}
			}
			else
			{
				var methodName = asyncMethodName;

				if (methodSymbol.TypeParameters.Length > 0)
				{
					var typeParams = string.Join(", ", methodSymbol.TypeParameters.Select(c => c.ToString()));

					methodName += "<" + typeParams + ">";
				}

				var callAsyncWithCancellationToken = SyntaxFactory.InvocationExpression
				(
					SyntaxFactory.IdentifierName(methodName),
					SyntaxFactory.ArgumentList
					(
						new SeparatedSyntaxList<ArgumentSyntax>()
						.AddRange(methodSymbol.Parameters.TakeWhile(c => !(c.HasExplicitDefaultValue || c.IsParams)).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
						.Add(SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName("CancellationToken"), SyntaxFactory.IdentifierName("None"))))
						.AddRange(methodSymbol.Parameters.SkipWhile(c => !(c.HasExplicitDefaultValue || c.IsParams)).Select(c => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(c.Name))))
					)
				);

				if (!(isInterfaceMethod || methodSymbol.IsAbstract))
				{
					newAsyncMethod = newAsyncMethod.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(callAsyncWithCancellationToken)));
				}
			}

			if (!(isInterfaceMethod || methodSymbol.IsAbstract))
			{
				var baseAsyncMethod = this
					.GetMethods(semanticModel, methodSymbol.ReceiverType.BaseType, methodSymbol.Name + "Async", newAsyncMethod, methodSyntax)
					.FirstOrDefault();

				var baseMethod = this
					.GetMethods(semanticModel, methodSymbol.ReceiverType.BaseType, methodSymbol.Name, methodSyntax, methodSyntax)
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

				if (parentContainsAsyncMethod && !(baseAsyncMethod.IsVirtual || baseAsyncMethod.IsAbstract || baseAsyncMethod.IsOverride))
				{
					return null;
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
