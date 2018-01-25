// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.ExpressionWriter
{
	public class ExpressionHasherWriter
	{
		private readonly string[] paths;

		private ExpressionHasherWriter(string[] paths)
		{
			this.paths = paths;
		}
		
		private IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol type,bool inherited = true)
		{
			return this.GetProperties(type, new HashSet<string>(), inherited);
		}

		private IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol type, HashSet<string> alreadyAdded, bool inherited = true)
		{
			foreach (var member in type.GetMembers())
			{

				if (member is IPropertySymbol property && property.ExplicitInterfaceImplementations.Length == 0 && !(alreadyAdded.Contains(property.Name) && (property.IsAbstract || property.IsVirtual || property.IsOverride))
					&& property.DeclaredAccessibility == Accessibility.Public)
				{
					alreadyAdded.Add(property.Name);

					yield return property;
				}
			}

			if (type.BaseType != null && inherited)
			{
				foreach (var member in this.GetProperties(type.BaseType, alreadyAdded))
				{
					yield return member;
				}
			}
		}

		private IEnumerable<IMethodSymbol> GetVisitMethods(INamedTypeSymbol type, bool inherited = true)
		{
			foreach (var member in type.GetMembers())
			{

				if (member is IMethodSymbol method)
				{
					if ((method.IsOverride || method.IsVirtual) && method.Name.StartsWith("Visit") && method.Parameters.Length == 1)
					{
						yield return method;
					}
				}
			}

			if (type.BaseType != null && inherited)
			{
				foreach (var member in this.GetVisitMethods(type.BaseType))
				{
					yield return member;
				}
			}
		}

		private bool IsOfType(ITypeSymbol type, string typeName)
		{
			if (type.Name == typeName)
			{
				return true;
			}

			foreach (var i in type.AllInterfaces)
			{
				if (i.Name == typeName)
				{
					return true;
				}
			}

			if (type.BaseType != null)
			{
				return this.IsOfType(type.BaseType, typeName);
			}

			return false;
		}

		private BlockSyntax CreateMethodBody(SemanticModel model, INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
		{
			var allVisitMethods = this.GetVisitMethods(typeSymbol).ToList();
			var expressionParam = SyntaxFactory.IdentifierName("expression");
			var properties = this.GetProperties((INamedTypeSymbol)methodSymbol.Parameters.First().Type).Where(c => c.Name != "CanReduce" && c.Name != "Type" && c.Name != "NodeType").ToList();

			var simpleProperties = properties
				.Where(c => !this.IsOfType(c.Type, "Expression") && !this.IsOfType(c.Type, "IReadOnlyList") && allVisitMethods.All(d => d.Parameters[0].Type.Name != c.Type.Name))
				.ToList();

			if (simpleProperties.Count == 0)
			{
				return null;
			}

			var hashCode = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName("hashCode"));
			
			var simplePropertyHashing = simpleProperties.Select(c =>
			{
				var type = c.Type;

				if (type.IsValueType)
				{
					if (type.Name == "Boolean")
					{
						return SyntaxFactory.ConditionalExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(c.Name.GetHashCode())), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
					}
					else
					{
						return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)), SyntaxFactory.IdentifierName("GetHashCode")));
					}
				}

				return (ExpressionSyntax)SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, SyntaxFactory.InvocationExpression(SyntaxFactory.ConditionalAccessExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)), SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("GetHashCode")))), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
			})
			.Select(c => SyntaxFactory.AssignmentExpression(SyntaxKind.ExclusiveOrAssignmentExpression, hashCode, c))
			.Select(SyntaxFactory.ExpressionStatement)
			.ToArray<StatementSyntax>();

			return SyntaxFactory
				.Block()
				.AddStatements(simplePropertyHashing)
				.AddStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.BaseExpression(), SyntaxFactory.IdentifierName(methodSymbol.Name)), SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(expressionParam)))));
		}

		private string Write()
		{
			var syntaxTrees = this.paths.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p))).ToList();
			var compilation = CSharpCompilation.Create("Temp", syntaxTrees, null, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddReferences
				(
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Stream).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(ExpressionType).Assembly.Location)
				);

			var expressionComparerType = syntaxTrees.SelectMany(c => c.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(d => d.Identifier.Text == "SqlExpressionHasher")).Single();
			var model = compilation.GetSemanticModel(expressionComparerType.SyntaxTree, true);
			var syntaxTree = expressionComparerType.SyntaxTree;

			var symbol = model.GetDeclaredSymbol(expressionComparerType);

			var visitMethods = this.GetVisitMethods(symbol).Where(c => c.Parameters.Single().Type.AllInterfaces.Concat(new[] { c.Parameters.Single().Type }).All(d => d.Name != "IReadOnlyList") && c.Parameters.Single().Type.Name != typeof(Expression).Name).ToList();
			var visitMethodsToIgnore = this.GetVisitMethods(symbol, false).ToList();

			visitMethods.RemoveAll(c => visitMethodsToIgnore.Any(d => d.Name == c.Name && d.Parameters.SequenceEqual(c.Parameters, (x, y) => x.Type.Name == y.Type.Name)));
			
			SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit().WithUsings(expressionComparerType.SyntaxTree.GetCompilationUnitRoot().Usings));

			var usings = syntaxTree.GetCompilationUnitRoot().Usings;
			
			usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true))));

			var methods = visitMethods.Select(c =>
			{
				var body = this.CreateMethodBody(model, symbol, c);

				if (body == null)
				{
					return null;
				}

				return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(c.ReturnType.Name), c.Name)
					.WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(c.DeclaredAccessibility == Accessibility.Public ? SyntaxKind.PublicKeyword : SyntaxKind.ProtectedKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
					.WithParameterList(SyntaxFactory.ParameterList(new SeparatedSyntaxList<ParameterSyntax>().AddRange(c.Parameters.Select(d => SyntaxFactory.Parameter(SyntaxFactory.Identifier("expression")).WithType(SyntaxFactory.IdentifierName(d.Type.Name))))))
					.WithBody(body);
			});

			var namespaces = SyntaxFactory.List<MemberDeclarationSyntax>
			(
				syntaxTree.GetRoot()
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>()
					.GroupBy(m => m.FirstAncestorOrSelf<ClassDeclarationSyntax>())
					.GroupBy(g => g.Key.FirstAncestorOrSelf<NamespaceDeclarationSyntax>())
					.Select
					(
						nsGrp => SyntaxFactory.NamespaceDeclaration(nsGrp.Key.Name).WithMembers
						(
							SyntaxFactory.List<MemberDeclarationSyntax>
							(
								nsGrp.Select
								(
									clsGrp => SyntaxFactory.ClassDeclaration(clsGrp.Key.Identifier)
										.WithModifiers(clsGrp.Key.Modifiers)
										.WithTypeParameterList(clsGrp.Key.TypeParameterList)
										.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(methods.Where(c => c != null)))
								)
							)
						)
					)
			);

			var result = SyntaxFactory.SyntaxTree
			(
				SyntaxFactory.CompilationUnit()
					.WithUsings(SyntaxFactory.List(usings))
					.WithMembers(namespaces)
					.WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
					.NormalizeWhitespace()
			);

			return result.ToString();
		}

		public static string Write(string[] sourcePaths)
		{
			return new ExpressionHasherWriter(sourcePaths).Write();
		}

		public static void Write(string[] sourcePaths, string outputFile)
		{
			var result = new ExpressionHasherWriter(sourcePaths).Write();

			File.WriteAllText(outputFile, result);
		}
	}
}
