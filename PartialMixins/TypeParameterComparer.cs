using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace PartialMixins
{
    internal class TypeParameterComparer : IEqualityComparer<ITypeParameterSymbol>
    {
        public bool Equals(ITypeParameterSymbol x, ITypeParameterSymbol y)
        {
            return x.DeclaringType.ContainingNamespace.ToDisplayString() == y.DeclaringType.ContainingNamespace.ToDisplayString()
                && x.DeclaringType.MetadataName == y.DeclaringType.MetadataName
                && x.MetadataName == y.MetadataName;
        }

        public int GetHashCode(ITypeParameterSymbol obj)
        {
            return (obj.DeclaringType.ContainingNamespace.ToDisplayString().GetHashCode() ^
                             31 * obj.DeclaringType.MetadataName.GetHashCode()) ^
                            31 * obj.MetadataName.GetHashCode();


        }
    }
}