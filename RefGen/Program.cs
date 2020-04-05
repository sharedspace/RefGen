using RefGen.IEqualityComparers;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefGen
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    new []{"--input", "-i"}, 
                    "Input Assembly")
                {
                    Required = true
                }.ExistingOnly(),
                new Option<DirectoryInfo[]>(
                    new []{"--reference-assembly-dirs" }, 
                    "Directories containing Reference Assemblies")
                .ExistingOnly(),
                new Option<FileInfo[]>(
                    new []{"-r", "--reference-assemblies" },
                    "List of Reference Assemblies")
                .ExistingOnly(),
                new Option<DirectoryInfo>(
                    new []{"--output", "-o"},
                    "Output Directory")
                {
                    Required = true
                },
                new Option<bool>(
                    new[]{"--zap-output", "-z"},
                    "Delete and recreate output folder if it already exists")
            };

            rootCommand.Description = "Reference Assembly Generator";
            rootCommand.Handler = 
                CommandHandler.Create<
                    FileSystemInfo, 
                    DirectoryInfo[], 
                    FileInfo[], 
                    DirectoryInfo,
                    bool>(ProcessCommandLineArgs);
            return rootCommand.Invoke(args);
        }

        private static void ProcessCommandLineArgs(
            FileSystemInfo input, 
            DirectoryInfo[] referenceAssemblyDirs,
            FileInfo[] referenceAssemblies,
            DirectoryInfo output,
            bool zapOutput)
        {
            if (!EnsureOutput(output, zapOutput))
            {
                return;
            }

            ReferenceGenerator.Generate(input, output, referenceAssemblyDirs, referenceAssemblies);
        }

        private static bool EnsureOutput(DirectoryInfo output, bool zapOutput)
        {
            if (zapOutput && output?.FullName != null && Directory.Exists(output.FullName))
            {
                output.Delete(recursive: true);
            }

            if (output?.FullName != null && !Directory.Exists(output.FullName))
            {
                output.Create();
            }

            return output?.FullName != null && Directory.Exists(output.FullName);
        }
    }
}
