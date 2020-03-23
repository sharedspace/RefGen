using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace RefGen.IEqualityComparers
{
    internal class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        public bool Equals([AllowNull] TypeReference x, [AllowNull] TypeReference y)
        {
            return AreSame(x, y);
        }

        private static bool AreSame([AllowNull] TypeReference a, [AllowNull] TypeReference b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            if (a.MetadataType != b.MetadataType)
                return false;

            if (a.IsGenericParameter)
            {
                return ((GenericParameter)a).Position == ((GenericParameter)b).Position;
            }

            if (a.IsTypeSpecification())
                return AreSame((TypeSpecification)a, (TypeSpecification)b);

            if (a.Name != b.Name || a.Namespace != b.Namespace)
                return false;

            //TODO: check scope

            return AreSame(a.DeclaringType, b.DeclaringType);
        }

        private static bool AreSame(TypeSpecification a, TypeSpecification b)
        {
            if (!AreSame(a.ElementType, b.ElementType))
                return false;

            if (a.IsGenericInstance)
                return AreSame((GenericInstanceType)a, (GenericInstanceType)b);

            if (a.IsRequiredModifier || a.IsOptionalModifier)
                return AreSame((IModifierType)a, (IModifierType)b);

            if (a.IsArray)
                return AreSame((ArrayType)a, (ArrayType)b);

            return true;
        }

        private static bool AreSame(IModifierType a, IModifierType b)
        {
            return AreSame(a.ModifierType, b.ModifierType);
        }

        public int GetHashCode([DisallowNull] TypeReference obj) => obj.FullName.GetHashCode();

        public static IEqualityComparer<TypeReference> Comparer { get; } = new TypeReferenceEqualityComparer();

    }

    internal static partial class Extensions
    {
        public static bool IsTypeSpecification(this TypeReference type)
        {
            switch (type.MetadataType)
            {
                case MetadataType.Array:
                case MetadataType.ByReference:
                case MetadataType.OptionalModifier:
                case MetadataType.RequiredModifier:
                case MetadataType.FunctionPointer:
                case MetadataType.GenericInstance:
                case MetadataType.MVar:
                case MetadataType.Pinned:
                case MetadataType.Pointer:
                case (MetadataType)0x1d:     // ElementType.SzArray
                case MetadataType.Sentinel:
                case MetadataType.Var:
                    return true;
            }

            return false;
        }
    }
}
