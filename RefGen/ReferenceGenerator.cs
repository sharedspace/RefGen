using System;
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
        internal static void Generate(FileSystemInfo file)
        {
            try
            {
                using var scopedCopy = file.CreateTemporaryCopy();
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(file.FullName));

                var assemblyDefinition =
                    AssemblyDefinition.ReadAssembly(
                        file.FullName,
                        new ReaderParameters()
                        {
                            AssemblyResolver = assemblyResolver,
                            ReadSymbols = true
                        });

                assemblyDefinition.RemoveMethodBodies();
                assemblyDefinition.RemoveNonPublicTypes();
                assemblyDefinition.RemoveNonPublicNestedTypes();
                assemblyDefinition.RemoveNonPublicMethodsAndFields();
                assemblyDefinition.RemoveNonPublicProperties();
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