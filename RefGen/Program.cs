using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace RefGen
{
    class Program
    {
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    new string[] {"--input", "-i" },
                    "Input Assembly"
                    )
            };

            rootCommand.Description = "Reference Assembly Generator";
            rootCommand.Handler = CommandHandler.Create<FileInfo>(ProcessCommandLineArgs);
            return rootCommand.InvokeAsync(args).Result;
        }

        private static void ProcessCommandLineArgs([NotNull]FileInfo file)
        {
        }
    }
}
