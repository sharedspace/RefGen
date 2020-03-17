using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RefGen
{
    internal static class Extensions
    {
        internal static ScopedFileInfo CreateTemporaryCopy(this FileSystemInfo file)
        {
            int retries = 3;
            do
            {
                var tempFile = Path.GetTempFileName();
                File.Delete(tempFile);
                tempFile = Path.ChangeExtension(tempFile, Path.GetExtension(file.Name));
                if (!File.Exists(tempFile))
                {
                    File.Copy(file.FullName, tempFile);
                    return new ScopedFileInfo(tempFile);
                }
            } while (--retries > 0);

            throw new RetriesFailedException(nameof(CreateTemporaryCopy));
        }

        internal static void RemoveMethodBodies(this AssemblyDefinition def)
        {
            var tRefNotImplementedExceptionCtor =
                def.MainModule.ImportReference(
                    typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));

            foreach (var type in def.MainModule.Types)
            {
                foreach (var method in type.Methods)
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
                }
            }
        }

        internal static void RemoveMethodBodies2(this AssemblyDefinition def)
        {
            throw new NotImplementedException();
        }

    }
}
