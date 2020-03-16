using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Mono.Cecil;

namespace RefGen
{
    internal class ReferenceGenerator
    {
        internal static void Generate(FileSystemInfo file)
        {
            var assemblyDefinition = 
                AssemblyDefinition.ReadAssembly(
                    file.FullName, 
                    new ReaderParameters(ReadingMode.Immediate));

            Debug.Assert(assemblyDefinition.Modules.Count == 1);

            foreach (var t in assemblyDefinition.MainModule.Types)
            {
                Console.WriteLine($"Type={t.FullName} | IsPublic: {t.IsPublic} | IsNotPublic: {t.IsNotPublic} | IsNestedPublic: {t.IsNestedPublic}");
            }
                
        }
    }
}