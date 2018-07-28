// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

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
			else if (self.TypeKind == TypeKind.TypeParameter || other.TypeKind == TypeKind.TypeParameter)
			{
				var typeParameter = self as ITypeParameterSymbol ?? other as ITypeParameterSymbol;
				
				if (typeParameter.HasReferenceTypeConstraint && !other.IsReferenceType)
				{
					return false;
				}

				if (typeParameter.HasValueTypeConstraint && !other.IsValueType)
				{
					return false;
				}

				return typeParameter.ConstraintTypes.IsEmpty || typeParameter.ConstraintTypes.Any(c => EqualsToIgnoreGenericParameters(c, other));
			}

			if (self.MetadataName != other.MetadataName)
			{
				return false;
			}

			if (self.Kind == SymbolKind.NamedType && other.Kind == SymbolKind.NamedType)
			{
				if (!((INamedTypeSymbol)self)
					.TypeArguments
					.SequenceEqual(((INamedTypeSymbol)other).TypeArguments, EqualsToIgnoreGenericParametersEqualityComparer.Default))
				{
					return false;
				}
			}

			return true;
		}
		
		public static int? IsAssignableFrom(this ITypeSymbol self, ITypeSymbol other, int? depth)
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

			int? result;

			if ((result = other.ParentTypes().Select(c => IsAssignableFrom(self, c, (depth ?? 0) + 1)).FirstOrDefault()) > 0)
			{
				return result;
			}

			if ((result = other.Interfaces.Select(c => IsAssignableFrom(self, c, (depth ?? 0) + 1)).FirstOrDefault()) > 0)
			{
				return result;
			}

			return null;
		}
	}
}