using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RefGen
{
    internal static class Extensions
    {
        internal static ScopedFileInfo CreateTemporaryCopy(this FileSystemInfo file)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(CreateTemporaryCopy));
            Trace.IndentLevel++;

            const int max_retries = 3;
            int retries = max_retries;
            do
            {
                Trace.WriteLine($"Attempt #{max_retries - retries + 1}");
                var tempFile = Path.GetTempFileName();
                File.Delete(tempFile);
                tempFile = Path.ChangeExtension(tempFile, Path.GetExtension(file.Name));
                if (!File.Exists(tempFile))
                {
                    File.Copy(file.FullName, tempFile);
                    Trace.WriteLine($"Copied {file.FullName} into {tempFile}");
                    return new ScopedFileInfo(tempFile);
                }
            } while (--retries > 0);
            Trace.WriteLine($"Failed to copy {file.FullName} to a temp-file after {max_retries} retries");
            throw new RetriesFailedException(nameof(CreateTemporaryCopy));
        }

        internal static void RemoveMethodBodies(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveMethodBodies));
            Trace.IndentLevel++;

            var tRefNotImplementedExceptionCtor =
                def.MainModule.ImportReference(
                    typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));

            foreach (var type in def.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        var il = method.Body.GetILProcessor();
                        while (il.Body.Instructions.Count > 0)
                        {
                            il.RemoveAt(0);
                        }

                        il.Body.Variables.Clear();
                        il.Body.ExceptionHandlers.Clear();
                        il.Body.InitLocals = false;

                        il.Emit(OpCodes.Newobj, tRefNotImplementedExceptionCtor);
                        il.Emit(OpCodes.Throw);

                        Trace.WriteLine($"Removed {method.FullName}");
                    }
                }
            }
        }

        internal static void RemoveNonPublicTypes(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveNonPublicTypes));
            Trace.IndentLevel++;

            var entryPointType = def.EntryPoint?.DeclaringType;
            if (entryPointType != null)
            {
                Trace.WriteLine($"EntryPoint type identified: {entryPointType.FullName}");
            }

            var publicTypes =
                def.MainModule.Types.Where(
                    (t) => t.IsPublic || t.IsNestedPublic || t.IsNestedAssembly);
            var nonPublicTypes =
                def.MainModule.Types.Where((t) => !publicTypes.Contains(t)).ToList();

            foreach (var type in nonPublicTypes)
            {
                if (type == entryPointType)
                {
                    Trace.WriteLine($"Skipping EntryPoint type {type.FullName}");
                    continue;
                }

                try
                {
                    def.MainModule.Types.Remove(type);
                    Trace.WriteLine($"Removed {type.FullName}");
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        internal static void RemoveNonPublicNestedTypes(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveNonPublicNestedTypes));
            Trace.IndentLevel++;

            var typesWithNestedTypes = def.MainModule.Types.Where((t) => t.HasNestedTypes);
            foreach (var type in typesWithNestedTypes)
            {
                var nonPublicNestedTypes =
                    type.NestedTypes.Where((t) => !t.IsNestedPublic).ToList();
                foreach (var nestedType in nonPublicNestedTypes)
                {
                    type.NestedTypes.Remove(nestedType);
                    Trace.WriteLine($"Removed {nestedType.FullName}");
                }
            }
        }

        internal static void RemoveNonPublicMethodsAndFields(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveNonPublicMethodsAndFields));
            Trace.IndentLevel++;

            var entryPointMethod = def.EntryPoint;
            Trace.WriteLine($"EntryPointMethod: {entryPointMethod?.FullName ?? string.Empty}");

            foreach (var type in def.MainModule.Types)
            {
                var nonPublicMethods = 
                    type.Methods.Where((m) => !m.IsPublic).ToList();
                var nonPublicFields =
                    type.Fields.Where((f) => !f.IsPublic).ToList();

                nonPublicMethods.ForEach((m) => 
                {
                    if (m != entryPointMethod)
                    {
                        type.Methods.Remove(m);
                        Trace.WriteLine($"Removed {m.FullName}");
                    }
                });
                nonPublicFields.ForEach((f) => 
                {
                    type.Fields.Remove(f);
                    Trace.WriteLine($"Removed {f.FullName}");
                });
            }
        }

        internal static void ChangeToDll(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(ChangeToDll));
            Trace.IndentLevel++;

            def.MainModule.EntryPoint.DeclaringType.Methods.Remove(def.MainModule.EntryPoint);

            def.Modules.ToList().ForEach((m) => m.Kind = ModuleKind.Dll);
            def.Modules.ToList().ForEach((m) => m.EntryPoint = null);
            def.MainModule.EntryPoint = null;
            def.EntryPoint = null;

            Debug.Assert(def.MainModule.Kind == ModuleKind.Dll);
            Debug.Assert(def.MainModule.EntryPoint == null);
        }
    }
}
