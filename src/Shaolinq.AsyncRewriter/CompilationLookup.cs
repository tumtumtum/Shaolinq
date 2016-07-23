using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shaolinq.AsyncRewriter
{
	internal static class TypeSymbolExtensions
	{
		private static IEnumerable<ITypeSymbol> ParentTypes(this ITypeSymbol self)
		{
			self = self.BaseType;

			while (self != null)
			{
				yield return self;

				self = self.BaseType;
			}
		}

		internal class EqualsToIgnoreGenericParametersEqualityComparer : IEqualityComparer<ITypeSymbol>
		{
			public static readonly EqualsToIgnoreGenericParametersEqualityComparer Default = new EqualsToIgnoreGenericParametersEqualityComparer();

			public bool Equals(ITypeSymbol x, ITypeSymbol y)
			{
				return x.EqualsToIgnoreGenericParameters(y);
			}

			public int GetHashCode(ITypeSymbol obj)
			{
				return obj.MetadataName.GetHashCode();
			}
		}

		internal static bool EqualsToIgnoreGenericParameters(this ITypeSymbol self, ITypeSymbol other)
		{
			if (self == other)
			{
				return true;
			}

			if (self.TypeKind == TypeKind.TypeParameter && other.TypeKind == TypeKind.TypeParameter)
			{
				var left = (ITypeParameterSymbol)self;
				var right = (ITypeParameterSymbol)other;

				return left.HasReferenceTypeConstraint == right.HasReferenceTypeConstraint
						&& left.HasValueTypeConstraint == right.HasValueTypeConstraint
						&& left.HasConstructorConstraint == right.HasConstructorConstraint;
			}

			if (self.MetadataName != other.MetadataName)
			{
				return false;
			}

			if (self.Kind == SymbolKind.NamedType && other.Kind == SymbolKind.NamedType)
			{
				if (!((INamedTypeSymbol)self)
					.TypeArguments
					.OrderBy(c => c.TypeKind)
					.ThenBy(c => c.MetadataName)
					.SequenceEqual(((INamedTypeSymbol)other).TypeArguments
					.OrderBy(c => c.TypeKind)
					.ThenBy(c => c.MetadataName), EqualsToIgnoreGenericParametersEqualityComparer.Default))
				{
					return false;
				}
			}

			return true;
		}
		
		public static int IsAssignableFrom(this ITypeSymbol self, ITypeSymbol other, int depth)
		{
			if (self == other)
			{
				return depth;
			}

			if (self.TypeKind == TypeKind.TypeParameter && other.TypeKind == TypeKind.TypeParameter)
			{
				var left = (ITypeParameterSymbol)self;
				var right = (ITypeParameterSymbol)other;

				if (left.HasReferenceTypeConstraint == right.HasReferenceTypeConstraint
					&& left.HasValueTypeConstraint == right.HasValueTypeConstraint
					&& left.HasConstructorConstraint == right.HasConstructorConstraint)
				{
					return depth;
				}
			}

			if (self.Kind == SymbolKind.NamedType && other.Kind == SymbolKind.NamedType && self.MetadataName == other.MetadataName)
			{
				if (((INamedTypeSymbol)self).TypeParameters.SequenceEqual(((INamedTypeSymbol)other).TypeParameters))
				{
					return depth;
				}
			}

			int result;

			if ((result = other.ParentTypes().Select(c => IsAssignableFrom(self, c, depth + 1)).FirstOrDefault()) > 0)
			{
				return result;
			}

			if ((result = other.Interfaces.Select(c => IsAssignableFrom(self, c, depth + 1)).FirstOrDefault()) > 0)
			{
				return result;
			}

			return -1;
		}
	}

	public class CompilationLookup
	{
		private readonly CSharpCompilation compilation;
		private readonly Dictionary<string, List<IMethodSymbol>> extensionMethodsByName = new Dictionary<string, List<IMethodSymbol>>();

		public CompilationLookup(CSharpCompilation compilation)
		{
			this.compilation = compilation;

			this.Visit(compilation);
		}

		private void Visit(CSharpCompilation compilation)
		{
			Visit(compilation.GlobalNamespace);
		}

		private SymbolInfo? GetSymbolInfoOrNull(SemanticModel model, SyntaxNode syntaxNode)
		{
			try
			{
				return model.GetSymbolInfo(syntaxNode);
			}
			catch (Exception)
			{
				Console.Error.WriteLine($"Could not find symbol for {syntaxNode} in {syntaxNode.SyntaxTree.FilePath} at {syntaxNode.GetLocation().GetLineSpan().StartLinePosition}");

				return null;
			}
		}

		public ISymbol GetSymbol(SyntaxNode syntaxNode)
		{
			var retval = compilation.SyntaxTrees.Select(c => GetSymbolInfoOrNull(this.compilation.GetSemanticModel(c, true), syntaxNode)).FirstOrDefault()?.Symbol;

			var y = compilation.SyntaxTrees.Select(c => GetSymbolInfoOrNull(this.compilation.GetSemanticModel(c, true), syntaxNode)).FirstOrDefault()?.Symbol;

			if (retval != null)
			{
				return retval;
			}

			return null;
		}

		public List<IMethodSymbol> GetExtensionMethods(string name, ITypeSymbol type)
		{
			List<IMethodSymbol> methods;

			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (!extensionMethodsByName.TryGetValue(name, out methods))
			{
				return new List<IMethodSymbol>();
			}

			var retval = new List<KeyValuePair<IMethodSymbol, int>>();

			foreach (var method in methods)
			{
				var depth = method.Parameters[0].Type?.IsAssignableFrom(type, 0);

				if (depth >= 0)
				{
					retval.Add(new KeyValuePair<IMethodSymbol, int>(method, depth.Value));
				}
			}

			return retval.OrderBy(c => c.Key.ContainingAssembly.Equals(this.compilation.Assembly) ? 0 : c.Value + 1).Select(c => c.Key).ToList();
		}

		private void Visit(INamespaceSymbol nameSpace)
		{
			foreach (var type in nameSpace.GetTypeMembers())
			{
				Visit(type);
			}

			foreach (var innerNameSpace in nameSpace.GetNamespaceMembers())
			{
				Visit(innerNameSpace);
			}
		}

		private void Visit(ITypeSymbol type)
		{
			foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
			{
				Visit(method);
			}
		}

		private bool MethodIsPublicOrAccessibleFromCompilation(IMethodSymbol method)
		{
			if (method.DeclaredAccessibility == Accessibility.Public)
			{
				return true;
			}

			if ((method.DeclaredAccessibility == Accessibility.Internal
				|| method.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
				&& Equals(method.ContainingAssembly, this.compilation.Assembly))
			{
				return true;
			}

			return false;
		}

		private void Visit(IMethodSymbol method)
		{
			if (method.Name.EndsWith("Async") 
				&& method.ReturnType.Name == "Task"
				&& method.IsExtensionMethod
				&& MethodIsPublicOrAccessibleFromCompilation(method))
			{
				List<IMethodSymbol> methods;

				if (extensionMethodsByName.TryGetValue(method.Name + "Async", out methods))
				{
					methods.Add(method);
				}
				else
				{
					extensionMethodsByName[method.Name] = new List<IMethodSymbol> { method };
				}
			}
		}
	}
}
