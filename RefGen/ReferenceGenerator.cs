using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace RefGen
{
    internal class ReferenceGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <remarks>
        ///     - Copy to temp file
        ///         - Remove bodies of all methods, getters, setters 
        ///         - Remove all non-public types
        ///         - Remove all non-public methods, getters, setters, fields, etc.
        ///         - Save
        ///         - Decompile
        ///     - Delete temp file
        /// </remarks>
        internal static void Generate(
            FileSystemInfo file, 
            DirectoryInfo d, 
            IReadOnlyCollection<DirectoryInfo> refAssemblyDirs, 
            IReadOnlyCollection<FileInfo> referenceAssemblies)
        {
            try
            {
                using var scopedCopy = file.CreateTemporaryCopy();

                var assemblyDefinition =
                    AssemblyDefinition.ReadAssembly(
                        file.FullName,
                        new ReaderParameters()
                        {
                            AssemblyResolver = new PathAssemblyResolver(
                                referenceAssemblies,
                                refAssemblyDirs,
                                new FileInfo(file.FullName).Directory),
                            ReadSymbols = true
                        });

                assemblyDefinition.RemoveMethodBodies();
                assemblyDefinition.RemoveNonPublicTypes();
                assemblyDefinition.RemoveNonPublicNestedTypes();
                assemblyDefinition.RemoveNonPublicBaseTypesAndInterfaces();
                assemblyDefinition.RemoveNonPublicMethodsAndFields();
                assemblyDefinition.RemoveNonPublicProperties();
                assemblyDefinition.RemoveAttributesWithNonPublicTypeRefs();
                assemblyDefinition.RemoveFieldInitializers();
                assemblyDefinition.RemoveResources();
                assemblyDefinition.RemoveCommonAssemblyAttributes();

                assemblyDefinition.MainModule.Attributes = ModuleAttributes.ILOnly;

                if (assemblyDefinition.CheckConsistency())
                {
                    assemblyDefinition.Write(
                        scopedCopy.FullName,
                        new WriterParameters()
                        {
                            WriteSymbols = true
                        });
                    Console.WriteLine(scopedCopy.FullName);
                    Trace.WriteLine(scopedCopy.FullName);

                    scopedCopy.Decompile(d);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine(e.ToString());
                Trace.WriteLine(e.ToString());
            }
        }
    }
}