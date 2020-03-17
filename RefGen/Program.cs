using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RefGen
{
    class Program
    {
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(new string[]{"-i" , "--input"}).ExistingOnly()
            };

            rootCommand.Description = "Reference Assembly Generator";
            rootCommand.Handler = CommandHandler.Create<FileSystemInfo>(ProcessCommandLineArgs);
            return rootCommand.Invoke(args);
        }

        private static void ProcessCommandLineArgs(FileSystemInfo i)
        {
            ReferenceGenerator.Generate(i);
        }
    }
}
