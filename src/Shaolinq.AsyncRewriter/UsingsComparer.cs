// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shaolinq.AsyncRewriter
{
	internal class UsingsComparer
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
}