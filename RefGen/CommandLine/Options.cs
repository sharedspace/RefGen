using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

namespace RefGen.CommandLine
{
    internal class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input assembly path")]
        public string InputAssembly { get; set; }


        [Option('o', "output", Required = false, HelpText = "Reference assembly location (optional)" )]
        public string ReferenceAssembly { get; set; }

        [Option(
            'a', 
            "accessModifiers", 
            Default = new string[] { "public", "internal", "protected" }, 
            Separator = '+', 
            HelpText = "Access Modifiers to Include (separate mulitple values with '+', for e.g., \"public+internal\")" )]
        public IEnumerable<string> AccessModifiers { get; set; }

    }
}
