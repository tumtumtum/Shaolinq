using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shaolinq.AsyncRewriter
{
	public static class TypeSymbolExtensions
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

		private static IEnumerable<ITypeSymbol> SelfAndParentTypes(this ITypeSymbol self)
		{
			while (self != null)
			{
				yield return self;

				self = self.BaseType;
			}
		}

		public class EqualsToIgnoreGenericParametersEqualityComparer : IEqualityComparer<INamedTypeSymbol>
		{
			public static readonly EqualsToIgnoreGenericParametersEqualityComparer Default = new EqualsToIgnoreGenericParametersEqualityComparer();

			public bool Equals(INamedTypeSymbol x, INamedTypeSymbol y)
			{
				if (x.IsGenericType && y.IsGenericType)
				{
					return true;
				}

				return x.EqualsToIgnoreGenericParameters(y);
			}

			public int GetHashCode(INamedTypeSymbol obj)
			{
				return obj.MetadataName.GetHashCode();
			}
		}

		public static bool EqualsToIgnoreGenericParameters(this INamedTypeSymbol self, INamedTypeSymbol other)
		{
			if (self == other)
			{
				return true;
			}

			if (self.MetadataName != other.MetadataName)
			{
				return false;
			}

			if (!self.TypeArguments.Cast<INamedTypeSymbol>().SequenceEqual(other.TypeParameters.Cast<INamedTypeSymbol>(), EqualsToIgnoreGenericParametersEqualityComparer.Default))
			{
				return false;
			}

			return true;
		}
		
		public static int IsAssignableFrom(this INamedTypeSymbol self, INamedTypeSymbol other, int depth)
		{
			if (self == other)
			{
				return depth;
			}

			if (self.MetadataName == other.MetadataName)
			{
				if (self.TypeParameters.SequenceEqual(other.TypeParameters))
				{
					return depth;
				}
			}

			int result;

			if ((result = other.ParentTypes().Select(c => IsAssignableFrom(self, (INamedTypeSymbol)c, depth + 1)).FirstOrDefault()) > 0)
			{
				return result;
			}

			if ((result = other.Interfaces.Select(c => IsAssignableFrom(self, (INamedTypeSymbol)c, depth + 1)).FirstOrDefault()) > 0)
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
				var retval = model.GetSymbolInfo(syntaxNode);
				
				if (retval.Symbol == null)
				{
					//this.compilation.GetSymbolsWithName(c => c == "");
					var y = model.GetSpeculativeSymbolInfo(syntaxNode.Parent.Parent.Parent.SpanStart, syntaxNode, SpeculativeBindingOption.BindAsExpression);

					var z = model.GetSpeculativeSymbolInfo(syntaxNode.Parent.Parent.SpanStart, syntaxNode, SpeculativeBindingOption.BindAsExpression);
				}

				return retval;
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

		public List<IMethodSymbol> GetExtensionMethods(string name, INamedTypeSymbol type)
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
				var depth = (method.Parameters[0].Type as INamedTypeSymbol)?.IsAssignableFrom(type, 0);

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

		private void Visit(IMethodSymbol method)
		{
			if (method.Name.EndsWith("Async") 
				&& method.ReturnType.Name == "Task"
				&& method.IsExtensionMethod)
				//&& method.DeclaredAccessibility == Accessibility.Public)
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
