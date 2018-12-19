using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using RefGen.CommandLine;
using RefGen.Module;

namespace RefGen
{
    internal class RefAssemblyGenerator
    {
        private string inputAssembly;
        private string referenceAssembly;
        private AccessModifier accessModifier;

        private ModuleDefMD inputModule;

        public RefAssemblyGenerator(string inputAssembly, string referenceAssembly, AccessModifier accessModifier)
        {
            this.inputAssembly = inputAssembly;
            this.referenceAssembly = referenceAssembly;
            this.accessModifier = accessModifier;

            inputModule = ModuleDefMD.Load(inputAssembly);
        }

        internal void Generate()
        {
            RemovePrivateMembers();
        }

        /// <summary>
        /// Remove private methods, properties and events
        /// </summary>
        private void RemovePrivateMembers()
        {
            foreach (var type in inputModule.GetAllTypes())
            {
                var methods = type.Methods.Where(m => m.IsInternal()).ToList();
            }
        }
    }
}