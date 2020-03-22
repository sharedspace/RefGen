using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace RefGen.IEqualityComparers
{
    public class DirectoryInfoComparer : IEqualityComparer<DirectoryInfo>
    {
        public bool Equals([AllowNull] DirectoryInfo x, [AllowNull] DirectoryInfo y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.FullName.Equals(y.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] DirectoryInfo obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.FullName.GetHashCode();
        }

        public static IEqualityComparer<DirectoryInfo> Comparer { get; } = new DirectoryInfoComparer();
    }
}
