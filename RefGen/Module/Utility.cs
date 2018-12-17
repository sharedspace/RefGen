using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RefGen.Module
{
    public static class Utility
    {
        /// <summary>
        /// Adds [InternalsVisibleTo("otherModule")] in sourceModule, and creates an updated version at <paramref name="modifiedSourceModule"/>
        /// </summary>
        /// <param name="sourceModule"></param>
        /// <param name="otherModule"></param>
        /// <param name="modifiedSourceModule"></param>
        public static void MakeInternalsVisibleTo(string sourceModule, string otherModule, string modifiedSourceModule)
        {
            if (!File.Exists(sourceModule))
            {
                throw new ArgumentException("Missing source module", nameof(sourceModule));
            }

            if (!File.Exists(otherModule))
            {
                throw new ArgumentException("Missing module", nameof(otherModule));
            }

            if (File.Exists(modifiedSourceModule))
            {
                throw new ArgumentException("Modified source module already exists", nameof(modifiedSourceModule));
            }

            var modOther = ModuleDefMD.Load(otherModule);
            var modSource = ModuleDefMD.Load(sourceModule);

            modSource.MakeInternalsVisibleTo(modOther);

            if (modSource.IsILOnly)
            {
                modSource.Write(modifiedSourceModule);
            }
            else
            {
                modSource.NativeWrite(modifiedSourceModule);
            }
        }
    }
}
