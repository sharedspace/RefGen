using Mono.Cecil;
using RefGen.IEqualityComparers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RefGen
{
    internal class PathAssemblyResolver: DefaultAssemblyResolver
    {
        public PathAssemblyResolver(IReadOnlyCollection<FileInfo> refAssemblies, IReadOnlyCollection<DirectoryInfo> refAssemblyDirs, params DirectoryInfo[] additionalReferenceAssemblyDirs)
        {
            if (refAssemblyDirs != null)
            {
                foreach (var dir in refAssemblyDirs)
                {
                    AddSearchDirectory(dir.FullName);
                }
            }

            if (refAssemblies != null)
            {
                foreach (var dir in 
                    new HashSet<DirectoryInfo>(
                        refAssemblies.Select(a => a.Directory), 
                        DirectoryInfoComparer.Comparer))
                {
                    AddSearchDirectory(dir.FullName);
                }
            }

            if (additionalReferenceAssemblyDirs != null)
            {
                foreach (var dir in additionalReferenceAssemblyDirs)
                {
                    AddSearchDirectory(dir.FullName);
                }
            }
        }

        
    }
}
