using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

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

                        Trace.WriteLine($"Stubbed {method.FullName}");
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

                    if (def.MainModule.GetMemberReferences().Where(m => m.DeclaringType == type).ToList().Count != 0)
                    {
                        Trace.WriteLine($"WARNING: {type.FullName} has dangling references");
                    }

                    Trace.WriteLine($"Removed {type.FullName}");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
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
                    if (def.MainModule.GetMemberReferences().Where(m => m.DeclaringType == nestedType).ToList().Count != 0)
                    {
                        Trace.WriteLine($"WARNING: {nestedType.FullName} has dangling references");
                    }
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
                        Trace.WriteLine($"Removed {m?.DeclaringType?.FullName ?? "Unknown"} | {m.FullName}");
                        if (def.MainModule.GetMemberReferences().Where(mr => mr == m).ToList().Count != 0)
                        {
                            Trace.WriteLine($"WARNING: {m.FullName} has dangling references");
                        }
                    }
                });
                nonPublicFields.ForEach((f) => 
                {
                    type.Fields.Remove(f);
                    Trace.WriteLine($"Removed {f?.DeclaringType?.FullName ?? "Unknown"} | {f.FullName}");
                    if (def.MainModule.GetMemberReferences().Where(mr => mr == f).ToList().Count != 0)
                    {
                        Trace.WriteLine($"WARNING: {f.FullName} has dangling references");
                    }
                });
            }
        }

        internal static void RemoveFieldInitializers(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveFieldInitializers));
            def.MainModule.Types.ToList().ForEach(t => t.RemoveFieldInitializers());
        }

        internal static void RemoveFieldInitializers(this TypeDefinition type)
        {
            Trace.Indent();
            foreach (var field in type.Fields)
            {
                if (field.Constant == null)
                {
                    field.InitialValue = null;
                    Trace.WriteLine($"Removed: {field.DeclaringType.FullName} | {field.FullName}");
                }
                else
                {
                    Trace.WriteLine($"Skipping const field: {field.DeclaringType.FullName} | {field.FullName}={field.Constant}");
                }
            }

            type.NestedTypes?.ToList()?.ForEach(t => t.RemoveFieldInitializers());
            Trace.Unindent();
        }

        internal static void RemoveNonPublicProperties (this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveNonPublicProperties));
            foreach (var type in def.MainModule.Types)
            {
                type.RemoveNonPublicProperties();
            }
        }

        internal static void RemoveNonPublicProperties(this TypeDefinition type)
        {
            Trace.Indent();
            try
            {
                var propertiesToRemove = new List<PropertyDefinition>();
                foreach (var p in type.Properties)
                {
                    if (!p.GetMethod?.IsPublic == true)
                    {
                        p.GetMethod = null;
                        Trace.WriteLine($"Removed: {type.FullName} | {p.FullName}_get()");
                    }

                    if (!p.SetMethod?.IsPublic == true)
                    {
                        p.SetMethod = null;
                        Trace.WriteLine($"Removed: {type.FullName} | {p.FullName}_set()");
                    }

                    if (p.GetMethod == null && p.SetMethod == null)
                    {
                        propertiesToRemove.Add(p);
                    }
                }

                propertiesToRemove.ForEach(p => 
                {
                    type.Properties.Remove(p);
                    Trace.WriteLine($"Removed: {type.FullName} | {p.FullName}");
                });
            }
            finally
            {
                Trace.Unindent();
            }
        }

        internal static void RemoveResources(this AssemblyDefinition def)
        {
            if (def.MainModule.HasResources)
            {
                def.MainModule.Resources.Clear();
            }
        }

        internal static void RemoveCommonAssemblyAttributes(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.Write(nameof(RemoveCommonAssemblyAttributes));
            Trace.Indent();

            var ava = def.MainModule.ImportReference(new TypeReference("System.Reflection", "AssemblyVersionAttribute", def.MainModule, def.MainModule.TypeSystem.CoreLibrary));
            var assemblyAttributesToIgnore = new List<TypeReference>
            {
                def.MainModule.ImportReference(typeof(AssemblyVersionAttribute)),
                def.MainModule.ImportReference(typeof(NeutralResourcesLanguageAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyVersionAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyTitleAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyProductAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyInformationalVersionAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyFileVersionAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyConfigurationAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyCompanyAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyMetadataAttribute)),
                def.MainModule.ImportReference(typeof(AssemblyDefaultAliasAttribute)),
                def.MainModule.ImportReference(typeof(DefaultDllImportSearchPathsAttribute)),
                def.MainModule.ImportReference(typeof(CLSCompliantAttribute)),
                def.MainModule.ImportReference(typeof(DependencyAttribute)),
                def.MainModule.ImportReference(typeof(DebuggableAttribute)),
                def.MainModule.ImportReference(typeof(RuntimeCompatibilityAttribute)),
                def.MainModule.ImportReference(typeof(CompilationRelaxationsAttribute)),
            }.AsReadOnly();

            var moduleAttributesToIgnore = new List<TypeReference>
            {
                def.MainModule.ImportReference(typeof(UnverifiableCodeAttribute)),
            }.AsReadOnly();

            var attributesToRemove =
                def
                .CustomAttributes
                .Where(a => assemblyAttributesToIgnore.Contains(a.AttributeType, TypeReferenceEqualityComparer.Comparer))
                .ToList();

            var moduleAttributesToRemove =
                def
                .MainModule
                .CustomAttributes
                .Where(a => moduleAttributesToIgnore.Contains(a.AttributeType, TypeReferenceEqualityComparer.Comparer))
                .ToList();

            attributesToRemove.ForEach(a => 
            { 
                def.CustomAttributes.Remove(a);
                Trace.WriteLine($"Removed [assembly:{a.AttributeType.FullName}]");
            });
            moduleAttributesToRemove.ForEach(a =>
            {
                def.MainModule.CustomAttributes.Remove(a);
                Trace.WriteLine($"Removed [module:{a.AttributeType.FullName}]");
            });
            Trace.Unindent();
        }


        internal static bool CheckConsistency(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(CheckConsistency));
            Trace.IndentLevel++;

            bool success = true;

            def
                .MainModule
                .GetTypeReferences()
                .Where(tr => tr.Scope == def.MainModule.Assembly.Name)
                .ToList()
                .ForEach((tr) => 
            {
                try
                {
                    tr.Resolve();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Trace.Write($"{e.GetType().Name}: {e.Message}");
                    Trace.WriteLine($"Failed to resolve TypeRef: {tr?.Scope?.Name ?? "Unknown"}:{tr.FullName}");
                    success = false;
                }
            });

            Trace.WriteLine($"Result: {success}");
            Trace.Unindent();
            return success;
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
