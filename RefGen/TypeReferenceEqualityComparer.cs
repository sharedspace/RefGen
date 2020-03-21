using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace RefGen
{
    internal class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        public bool Equals([AllowNull] TypeReference x, [AllowNull] TypeReference y)
        {
            if (x== null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (ReferenceEquals(x, y))
            {
                return true;
            }

            try
            {
                var tx = x.Resolve();
                var ty = y.Resolve();
                return tx == ty;
            }
            catch (ResolutionException e)
            {
                Trace.WriteLine($"Warning: TypeReference.Resolve() failed: {e.Message}");
                if (x.FullName.Equals(y.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetHashCode([DisallowNull] TypeReference obj) => obj.FullName.GetHashCode();

        public static IEqualityComparer<TypeReference> Comparer { get; } = new TypeReferenceEqualityComparer();
    }
}
