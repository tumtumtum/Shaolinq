// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	public static class MethodDeclarationSyntaxExtensions
	{
		public static MethodDeclarationSyntax WithAccessModifiers(this MethodDeclarationSyntax method, MethodAttributes methodAttributes)
		{
			if ((methodAttributes & (MethodAttributes.Public | MethodAttributes.Private | MethodAttributes.Family | MethodAttributes.Assembly)) == 0)
			{
				return method;
			}

			var kinds = new[] { SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.InternalKeyword };
			var tokens = method.Modifiers.Where(c => !kinds.Contains(c.Kind())).ToList();

			if ((methodAttributes & MethodAttributes.Public) != 0)
			{
				return method.WithModifiers(new SyntaxTokenList()
					.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
					.AddRange(tokens));
			}

			if ((methodAttributes & MethodAttributes.Family) != 0)
			{
				if ((methodAttributes & MethodAttributes.Assembly) != 0)
				{
					return method.WithModifiers(new SyntaxTokenList()
						.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
						.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
						.AddRange(tokens));
				}

				if ((methodAttributes & MethodAttributes.Assembly) != 0)
				{
					return method.WithModifiers(new SyntaxTokenList()
						.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
						.AddRange(tokens));
				}
			}

			if ((methodAttributes & MethodAttributes.Private) != 0)
			{
				return method.WithModifiers(new SyntaxTokenList()
					.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
					.AddRange(tokens));
			}

			return method.WithModifiers(new SyntaxTokenList().AddRange(tokens));
		}
	}
}
