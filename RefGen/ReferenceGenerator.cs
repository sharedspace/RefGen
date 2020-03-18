using System;
using System.Diagnostics;
using System.IO;
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
                using (var scopedCopy = file.CreateTemporaryCopy())
                {
                    var assemblyDefinition = AssemblyDefinition.ReadAssembly(file.FullName);
                    assemblyDefinition.RemoveMethodBodies();
                    assemblyDefinition.RemoveNonPublicTypes();
                    assemblyDefinition.RemoveNonPublicNestedTypes();
                    assemblyDefinition.RemoveNonPublicMethodsAndFields();

                    assemblyDefinition.MainModule.Attributes = ModuleAttributes.ILOnly;
                    

                    assemblyDefinition.Write(scopedCopy.FullName);

                    Console.WriteLine(scopedCopy.FullName);
                    Trace.WriteLine(scopedCopy.FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Trace.WriteLine(e.ToString());
            }
        }
    }
}