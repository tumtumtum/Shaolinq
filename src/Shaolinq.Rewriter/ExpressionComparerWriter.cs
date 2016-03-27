using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.Rewriter
{
	public class ExpressionComparerWriter
	{
		private readonly string[] paths;

		private ExpressionComparerWriter(string[] paths)
		{
			this.paths = paths;
		}

		private List<TypeDeclarationSyntax> GetExpressionTypes(IList<SyntaxTree> syntaxTrees)
		{
			return syntaxTrees
				.SelectMany(c => c.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Where(d => d.BaseList.Types.Any(e => e.ToString() == "SqlBaseExpression"))).ToList();
		}

		private bool InheritsFrom(INamedTypeSymbol symbol, string typeName)
		{
			if (symbol == null)
			{
				return false;
			}

			while (true)
			{
				if (symbol.Name== typeName)
				{
					return true;
				}

				if (symbol.BaseType != null)
				{
					symbol = symbol.BaseType;
					continue;
				}

				break;
			}

			return false;
		}

		private IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol type,bool inherited = true)
		{
			return GetProperties(type, new HashSet<string>(), inherited);
		}

		private IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol type, HashSet<string> alreadyAdded, bool inherited = true)
		{
			foreach (var member in type.GetMembers())
			{
				var property = member as IPropertySymbol;
				
				if (property != null && property.ExplicitInterfaceImplementations.Length == 0 && !(alreadyAdded.Contains(property.Name) && (property.IsAbstract || property.IsVirtual || property.IsOverride))
					&&  property.DeclaredAccessibility == Accessibility.Public)
				{
					alreadyAdded.Add(property.Name);

					yield return property;
				}
			}

			if (type.BaseType != null && inherited)
			{
				foreach (var member in GetProperties(type.BaseType, alreadyAdded))
				{
					yield return member;
				}
			}
		}

		private IEnumerable<IMethodSymbol> GetVisitMethods(INamedTypeSymbol type, bool inherited = true)
		{
			foreach (var member in type.GetMembers())
			{
				var method = member as IMethodSymbol;

				if (method != null)
				{
					if ((method.IsOverride || method.IsVirtual) && method.Name.StartsWith("Visit") && method.Parameters.Length == 1)
					{
						var param = method.Parameters[0];

						yield return method;
					}
				}
			}

			if (type.BaseType != null && inherited)
			{
				foreach (var member in GetVisitMethods(type.BaseType))
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
				return IsOfType(type.BaseType, typeName);
			}

			return false;
		}

		private BlockSyntax CreateMethodBody(SemanticModel model, INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
		{
			var currentDeclarator = SyntaxFactory.VariableDeclarator("current");
			var variableCurrent = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(methodSymbol.Parameters.First().Type.Name), new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(currentDeclarator));

			var allVisitMethods = GetVisitMethods(typeSymbol, true).ToList();

			var thisKeyword = SyntaxFactory.Token(SyntaxKind.ThisKeyword);
			var expressionParam = SyntaxFactory.IdentifierName("expression");
			var tryGetValue = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("TryGetCurrent"), SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(expressionParam), SyntaxFactory.Argument(SyntaxFactory.IdentifierName(currentDeclarator.Identifier.Text)).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))));

			var ifTryGetValue = SyntaxFactory
				.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, tryGetValue), SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expressionParam)));

			var properties = GetProperties((INamedTypeSymbol)methodSymbol.Parameters.First().Type).Where(c => c.Name != "CanReduce").ToList();

			var simpleProperties = properties
				.Where(c => !IsOfType(c.Type, "Expression") && !IsOfType(c.Type, "IReadOnlyList") && allVisitMethods.All(d => d.Parameters[0].Type.Name != c.Type.Name))
				.ToList();

			var expressionProperties = properties
				.Where(c => IsOfType(c.Type, "Expression") || allVisitMethods.Any(d => d.Parameters[0].Type.Name == c.Type.Name))
				.ToList();

			var collectionProperties = properties
				.Where(c => IsOfType(c.Type, "IReadOnlyList"))
				.ToList();

			var result = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName("result"));
			var currentObject = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName("currentObject"));

			var simplePropertyChecks = simpleProperties.Select(c =>
			{
				var type = c.Type;
				var fullname = type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
				
				if (type.IsValueType || fullname == "System.Type" || fullname == "System.Reflection.FieldInfo" || fullname == "System.Reflection.MethodInfo" || fullname == "System.Reflection.PropertyInfo")
				{
					return (ExpressionSyntax)SyntaxFactory.BinaryExpression
					(
						SyntaxKind.EqualsExpression,
						SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("current"), SyntaxFactory.IdentifierName(c.Name)),
						SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name))
					);
				}
				else
				{
					return SyntaxFactory.InvocationExpression
					(
						SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("object"), SyntaxFactory.IdentifierName("Equals")),
						SyntaxFactory.ArgumentList().AddArguments
						(
							SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("current"), SyntaxFactory.IdentifierName(c.Name))),
							SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)))
						)
					);
				}
			})
			.Select(c => SyntaxFactory.AssignmentExpression(SyntaxKind.AndAssignmentExpression, result, c))
			.Select(c => SyntaxFactory.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(c)), SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expressionParam))))
			.ToArray<StatementSyntax>();

			var visitMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName("Visit"));

			var expressionPropertyChecks = expressionProperties.Select(c => new StatementSyntax[]
			{
				SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, currentObject, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("current"), SyntaxFactory.IdentifierName(c.Name)))),
				SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(allVisitMethods.FirstOrDefault(d => d.Parameters[0].Type.Name == c.Type.Name) == null ? visitMethod : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName(allVisitMethods.FirstOrDefault(d => d.Parameters[0].Type.Name == c.Type.Name).Name)), SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)))))),
				SyntaxFactory.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, result), SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expressionParam))),
			})
			.SelectMany(c => c)
			.ToArray();

			var collectionPropertyChecks = collectionProperties.Select(c =>
			{
				var readonlyInterface = c.Type.AllInterfaces.Concat(new [] { c.Type }).Single(d => d.Name == "IReadOnlyList");
				var type = ((INamedTypeSymbol)readonlyInterface).TypeArguments[0];

				var visitMethodToCall = allVisitMethods.FirstOrDefault(d => d.Parameters[0].Type.AllInterfaces.Concat(new[] { d.Parameters[0].Type as INamedTypeSymbol }).Any(e => e.Name == "IReadOnlyList" && e.TypeArguments[0].Name == type.Name));

				if (visitMethodToCall == null)
				{
					if (IsOfType(type, "Expression"))
					{
						visitMethodToCall = allVisitMethods.FirstOrDefault(d => d.Parameters[0].Type.AllInterfaces.Concat(new[] { d.Parameters[0].Type as INamedTypeSymbol }).Any(e => e.Name == "IReadOnlyList" && e.TypeArguments[0].Name == nameof(Expression)));
					}
					else
					{
						visitMethodToCall = allVisitMethods.Where(d => d.Name.EndsWith("ObjectList")).FirstOrDefault(d => d.Parameters[0].Type.AllInterfaces.Concat(new[] { d.Parameters[0].Type as INamedTypeSymbol }).Any(e => e.Name == "IReadOnlyList" && e.TypeArguments[0].Name == "T"));
					}
				}

				var visitMethodMethodToCallExpr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("this"), SyntaxFactory.IdentifierName(visitMethodToCall.Name));

				return new StatementSyntax[]
				{
					SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, currentObject, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("current"), SyntaxFactory.IdentifierName(c.Name)))),
					SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(visitMethodMethodToCallExpr, SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("expression"), SyntaxFactory.IdentifierName(c.Name)))))),
					SyntaxFactory.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, result), SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expressionParam)))
				};
			})
			.SelectMany(c => c)
			.ToArray();
			
			return SyntaxFactory
				.Block()
				.AddStatements(SyntaxFactory.LocalDeclarationStatement(variableCurrent))
				.AddStatements(ifTryGetValue)
				.AddStatements(simplePropertyChecks)
				.AddStatements(expressionPropertyChecks)
				.AddStatements(collectionPropertyChecks)
				.AddStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, currentObject, SyntaxFactory.IdentifierName("current"))))
				.AddStatements(SyntaxFactory.ReturnStatement(expressionParam));
		}

		private string Write()
		{
			var syntaxTrees = paths.Select(p => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(p))).ToList();
			var compilation = CSharpCompilation.Create("Temp", syntaxTrees, null, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddReferences
				(
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Platform.Linq.ExpressionVisitor).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Stream).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(ExpressionType).Assembly.Location)
				);

			var expressionComparerType = syntaxTrees.SelectMany(c => c.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(d => d.Identifier.Text == "SqlExpressionComparer")).Single();
			var model = compilation.GetSemanticModel(expressionComparerType.SyntaxTree, true);
			var syntaxTree = expressionComparerType.SyntaxTree;

			var symbol = model.GetDeclaredSymbol(expressionComparerType);

			var visitMethods = GetVisitMethods(symbol).Where(c => c.Parameters.Single().Type.AllInterfaces.Concat(new[] { c.Parameters.Single().Type }).All(d => d.Name != "IReadOnlyList") && c.Parameters.Single().Type.Name != typeof(Expression).Name).ToList();
			var visitMethodsToIgnore = GetVisitMethods(symbol, false).ToList();

			visitMethods.RemoveAll(c => visitMethodsToIgnore.Any(d => d.Name == c.Name && d.Parameters.SequenceEqual(c.Parameters, (x, y) => x.Type.Name == y.Type.Name)));

			SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit().WithUsings(expressionComparerType.SyntaxTree.GetCompilationUnitRoot().Usings));

			var usings = syntaxTree.GetCompilationUnitRoot().Usings;
			
			usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true))));
			
			var methods = visitMethods.Select(c => 
				SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(c.ReturnType.Name), c.Name)
					.WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(c.DeclaredAccessibility == Accessibility.Public ? SyntaxKind.PublicKeyword : SyntaxKind.ProtectedKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
					.WithParameterList(SyntaxFactory.ParameterList(new SeparatedSyntaxList<ParameterSyntax>().AddRange(c.Parameters.Select(d => SyntaxFactory.Parameter(SyntaxFactory.Identifier("expression")).WithType(SyntaxFactory.IdentifierName(d.Type.Name))))))
					.WithBody(CreateMethodBody(model, symbol, c))
				);

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
									.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(methods))
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
			return new ExpressionComparerWriter(sourcePaths).Write();
		}
	}
}
