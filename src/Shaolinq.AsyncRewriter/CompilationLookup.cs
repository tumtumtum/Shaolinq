// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shaolinq.AsyncRewriter
{
	internal class CompilationLookup
	{
		private readonly Compilation compilation;
		private readonly Dictionary<string, List<IMethodSymbol>> extensionMethodsByName = new Dictionary<string, List<IMethodSymbol>>();

		public CompilationLookup(CSharpCompilation compilation)
		{
			this.compilation = compilation;
			
			Visit(compilation);
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

		public List<IMethodSymbol> GetExtensionMethods(string name, ITypeSymbol type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (!this.extensionMethodsByName.TryGetValue(name, out var methods))
			{
				return new List<IMethodSymbol>();
			}

			var retval = new List<KeyValuePair<IMethodSymbol, int>>();

			foreach (var method in methods)
			{
				var depth = method.Parameters[0].Type?.IsAssignableFrom(type, 0);

				if (depth > -1)
				{
					retval.Add(new KeyValuePair<IMethodSymbol, int>(method, depth.Value));
				}
			}

			return retval.OrderBy(c => c.Key.ContainingAssembly.Equals(this.compilation.Assembly) ? 0 : c.Value + 1).Select(c => c.Key).ToList();
		}
		
		private void Visit(Compilation compilationNode)
		{
			Visit(compilationNode.GlobalNamespace);
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

		private void Visit(INamespaceOrTypeSymbol type)
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
				&& method.IsExtensionMethod
				&& MethodIsPublicOrAccessibleFromCompilation(method))
			{
				if (this.extensionMethodsByName.TryGetValue(method.Name, out var methods))
				{
					methods.Add(method);
				}
				else
				{
					this.extensionMethodsByName[method.Name] = new List<IMethodSymbol> { method };
				}
			}
		}
	}
}
