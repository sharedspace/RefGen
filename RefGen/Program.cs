using CommandLine;
using dnlib.DotNet;
using dnlib.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using RefGen.Module;
using dnlib.DotNet.MD;
using RefGen.CommandLine;

namespace RefGen
{
    class Program
    {
        static void Main(string[] args)
        {
            new CommandLine.Semantics(args)
                .WithParsed(OnParsed)
                .WithNotParsed(OnParsingError);
        }

        private static void OnParsingError(IEnumerable<ArgumentException> errors)
        {
            foreach (var e in errors)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void OnParsed(CommandLine.Semantics options)
        {
            Console.WriteLine($"input: {options.InputAssembly}");
            Console.WriteLine($"ref assembly: {options.ReferenceAssembly}");
            Console.WriteLine($"access modifiers: {options.AccessModifier}");

            new RefAssemblyGenerator(options.InputAssembly, options.ReferenceAssembly, options.AccessModifier).Generate();

        }
    }
}
