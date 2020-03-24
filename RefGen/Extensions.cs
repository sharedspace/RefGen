using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RefGen.IEqualityComparers;
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
    internal static partial class Extensions
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

                        MethodDefinition ctorToCall = null;

                        if (method.IsConstructor && 
                            !method.IsStatic && 
                            type.BaseType?.Resolve()?.Methods?.Any(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0) != true)
                        {
                            // This is a constructor...
                            //  ... in a type whose base-class lacks a default constructor. 
                            // Therefore the first call in this method is likely a call to a constructor itself. 
                            //  Idenfify and preserve this call. 
                            var call = il.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Call);
                            if (call != null && 
                                call.Operand is MethodReference m && 
                                m.Resolve() is MethodDefinition md && 
                                md.IsConstructor)
                            {
                                // Generate a "dummy" call to this constructor 
                                if (md.IsPublic)
                                {
                                    ctorToCall = md;
                                }
                                else
                                {
                                    ctorToCall = 
                                        type
                                        .BaseType
                                        .Resolve()
                                        ?.Methods
                                        ?.FirstOrDefault(
                                            m => 
                                                m.IsConstructor && 
                                                !m.IsStatic && 
                                                (m.IsPublic || m.IsFamily));
                                }
                            }
                        }

                        while (il.Body.Instructions.Count > 0)
                        {
                            il.RemoveAt(0);
                        }

                        il.Body.Variables.Clear();
                        il.Body.ExceptionHandlers.Clear();
                        il.Body.InitLocals = false;

                        if (ctorToCall != null && ctorToCall.IsPublic)
                        {
                            if (ctorToCall.Parameters.Count > 0)
                            {
                                il.Emit(OpCodes.Ldarg_0);
                                foreach (var p in ctorToCall.Parameters)
                                {
                                    p.GenerateDefaultParameterValue(il);
                                }
                                il.Emit(OpCodes.Call, ctorToCall);
                                il.Emit(OpCodes.Nop);
                                il.Emit(OpCodes.Nop);
                            }
                        }

                        il.Emit(OpCodes.Newobj, tRefNotImplementedExceptionCtor);
                        il.Emit(OpCodes.Throw);

                        Trace.WriteLine($"Stubbed {method.FullName}");
                    }
                }
            }
        }

        private static void GenerateDefaultParameterValue(this ParameterDefinition p, ILProcessor il)
        {
            if (p.ParameterType.HasGenericParameters ||
                p.ParameterType.ContainsGenericParameter)
            {
                p.GenerateDefaultParameterValueForGenerics(il);
                return;
            }

            var parameterType = p.ParameterType.Resolve();
            if (!parameterType.IsValueType)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                switch (parameterType.MetadataType)
                {
                    case MetadataType.Boolean:
                    case MetadataType.Char:
                    case MetadataType.Int16:
                    case MetadataType.UInt16:
                    case MetadataType.Int32:
                    case MetadataType.UInt32:
                    case MetadataType.SByte:
                    case MetadataType.Byte:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case MetadataType.Int64:
                    case MetadataType.UInt64:
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_I8);
                        break;
                    case MetadataType.Single:
                        il.Emit(OpCodes.Ldc_R4, 0.0);
                        break;
                    case MetadataType.Double:
                        il.Emit(OpCodes.Ldc_R8, 0.0);
                        break;
                    case MetadataType.ByReference:
                        break;
                    case MetadataType.Array:
                        il.Emit(OpCodes.Ldnull);
                        break;
                    case MetadataType.IntPtr:
                    case MetadataType.UIntPtr:
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_I);
                        break;
                }
            }
        }

        private static void GenerateDefaultParameterValueForGenerics(this ParameterDefinition p, ILProcessor il)
        {

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
                    

                    //if (def
                    //    .MainModule
                    //    .GetMemberReferences()
                    //    .Where(m => m.DeclaringType.Resolve() == type)
                    //    .ToList()
                    //    .Count != 0)
                    //{
                    //    Trace.WriteLine($"WARNING: {type.FullName} has dangling references");
                    //}

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

        internal static void RemoveNonPublicBaseTypesAndInterfaces(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveNonPublicBaseTypesAndInterfaces));
            Trace.Indent();

            foreach (var type in def.MainModule.Types)
            {
                if (type.BaseType != null && 
                    !type.BaseType.Resolve().IsPublic)
                {
                    Trace.WriteLine($"Removing Base Type {type.BaseType.FullName} from {type.FullName}");
                    type.BaseType = null;
                }

                if (type.Interfaces != null)
                {
                    var nonPublicInterfaces = new List<InterfaceImplementation>();
                    foreach (var i in type.Interfaces)
                    {
                        if (!i.InterfaceType.Resolve().IsPublic)
                        {
                            nonPublicInterfaces.Add(i);
                        }
                    }
                    nonPublicInterfaces.ForEach(i => 
                    {
                        Trace.WriteLine($"Removing Interface {i.InterfaceType.FullName} from type {type.FullName}");
                        type.Interfaces.Remove(i);
                        var methodsToRemove =
                            type
                            .Methods
                            .Where(m =>
                                m.HasOverrides &&
                                m.Overrides.Any(o => o.Resolve().DeclaringType == i.InterfaceType))
                            .ToList();
                        methodsToRemove.ForEach(m =>
                        {
                            type.Methods.Remove(m);
                            Trace.WriteLine($"Removed Interface Method {m.FullName}");
                        });
                    });
                }

                Trace.Unindent();
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
                // Include public and protected methods/fields
                var nonPublicMethods = 
                    type.Methods.Where((m) => !m.IsPublic && !m.IsFamily && m.Overrides.Count == 0).ToList();
                var nonPublicFields =
                    type.Fields.Where((f) => !f.IsPublic && !f.IsFamily).ToList();

                nonPublicMethods.ForEach((m) => 
                {
                    if (m != entryPointMethod)
                    {
                        type.Methods.Remove(m);
                        Trace.WriteLine($"Removed {type.FullName} | {m.FullName}");
                        if (def.MainModule.GetMemberReferences().Where(mr => mr == m).ToList().Count != 0)
                        {
                            Trace.WriteLine($"WARNING: {m.FullName} has dangling references");
                        }
                    }
                });
                nonPublicFields.ForEach((f) => 
                {
                    type.Fields.Remove(f);
                    Trace.WriteLine($"Removed {type.FullName} | {f.FullName}");
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

        internal static void RemoveAttributesWithNonPublicTypeRefs(this AssemblyDefinition def)
        {
            Trace.IndentLevel = 0;
            Trace.WriteLine(nameof(RemoveAttributesWithNonPublicTypeRefs));
            Trace.Indent();
            foreach (var type in def.MainModule.Types)
            {
                if (type.HasCustomAttributes)
                {
                    var attrsToRemove = new List<CustomAttribute>();
                    foreach (var attr in type.CustomAttributes)
                    {
                        var attrType = attr.AttributeType.Resolve();
                        if (!attrType.IsPublic)
                        {
                            attrsToRemove.Add(attr);
                            continue;
                        }

                        if (attr.HasConstructorArguments)
                        {
                            var tType = def.MainModule.ImportReference(typeof(Type));
                            if (attr.ConstructorArguments.Any(ca =>
                            {
                                if (TypeReferenceEqualityComparer.Comparer.Equals(ca.Type,tType))
                                {
                                    // ca.Value is a System.Type object
                                    if (ca.Value is TypeReference caValueRef)
                                    {
                                        var caValue = caValueRef.Resolve();
                                        var isPublic =
                                            caValue != null && 
                                            ((caValue.IsPublic && !caValue.IsNested) ||
                                            (caValue.IsNestedPublic && caValue.IsNested));
                                        return !isPublic;
                                    }
                                }
                                return false;
                            }))
                            {
                                attrsToRemove.Add(attr);
                            }
                        }
                    }

                    attrsToRemove.ForEach(a =>
                    {
                        type.CustomAttributes.Remove(a);
                        Trace.WriteLine($"Removed CustomAttribute {a.AttributeType.FullName}");
                    });
                }
            }
            Trace.Unindent();
        }

        internal static void RemoveNonPublicProperties(this TypeDefinition type)
        {
            Trace.Indent();
            try
            {
                var propertiesToRemove = new List<PropertyDefinition>();
                foreach (var p in type.Properties)
                {

                    if (p.GetMethod != null && !p.GetMethod.IsPublic && !p.GetMethod.IsFamily && p.GetMethod.Overrides?.Count == 0)
                    {
                        p.GetMethod = null;
                        Trace.WriteLine($"Removed: {type.FullName} | {p.FullName}_get()");
                    }


                    if (p.SetMethod != null && !p.SetMethod.IsPublic && !p.SetMethod.IsFamily && p.SetMethod.Overrides?.Count == 0)
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

        internal static void Decompile(this ScopedFileInfo file, DirectoryInfo outputDirectory)
        {
            var decompiler = new WholeProjectDecompiler()
            {
                AssemblyResolver = new UniversalAssemblyResolver(file.Name, false, string.Empty)
            };
            using var peFile = new PEFile(file.FullName);
            decompiler.DecompileProject(peFile, outputDirectory.FullName);
        }
    }
}
