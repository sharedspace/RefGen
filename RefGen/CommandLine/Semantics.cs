using CommandLine;
using dnlib.DotNet;
using RefGen.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RefGen.CommandLine
{
    internal class Semantics
    {
        public string InputAssembly { get; private set; } = null;

        public string ReferenceAssembly { get; private set; } = null;

        public AccessModifier AccessModifier { get; private set; } = AccessModifier.None;

        public bool Success { get; private set; } = false;

        private TextWriter ErrorStream { get; } = Console.Out;

        private Parser Parser { get; }

        private string[] Args { get; }

        private List<ArgumentException> ParseExceptions { get; set; } = new List<ArgumentException>();

        public Semantics(string[] args)
        {
            Args = args;
            Parser = new Parser(config => config.HelpWriter = ErrorStream);
            ParseCommandLine();
        }

        private void ParseCommandLine()
        {
            var result = Parser.ParseArguments<Options>(Args)
                .WithParsed(OnParsed)
                .WithNotParsed(OnNotParsed);
        }

        private void OnNotParsed(IEnumerable<Error> errors)
        {
            ShowError();
        }

        private void OnParsed(Options options)
        {
            try
            {
                ParseInputAssembly(options);
            }
            catch(ArgumentException e)
            {
                ParseExceptions.Add(e);
            }

            try
            {
                ParseReferenceAssembly(options);
            }
            catch (ArgumentException e)
            {
                ParseExceptions.Add(e);
            }


            try
            {
                ParseAccessModifiers(options);
            }
            catch (ArgumentException e)
            {
                ParseExceptions.Add(e);
            }

            Success = ParseExceptions.Count == 0;
        }

        private void ParseAccessModifiers(Options options)
        {
            AccessModifier modifier = AccessModifier.None;
            foreach (var option in options.AccessModifiers)
            {
                if (Enum.TryParse(option, ignoreCase: true, result: out AccessModifier result))
                {
                    modifier |= result;
                }
                else
                {
                    throw new ArgumentException($"{option} is not a valid access modifier", "accessModifiers");
                }
            }

            AccessModifier = modifier;
        }

        private void ParseReferenceAssembly(Options options)
        {
            var outputAssembly = options.ReferenceAssembly;
            if (string.IsNullOrEmpty(outputAssembly))
            {
                if (string.IsNullOrEmpty(InputAssembly))
                {
                    throw new ArgumentException("outputAssembly is not specified in commandline and inputAssembly location not discovered", "output");
                }

                var inputAssemblyDirectory = Path.GetDirectoryName(InputAssembly);
                var refAssemblyDirectory = Path.Combine(inputAssemblyDirectory, "ref");
                if (!Directory.Exists(refAssemblyDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(refAssemblyDirectory);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PathTooLongException || e is NotSupportedException)
                    {
                        // Try a backup location
                        var refAssemblyDirectory2 =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ref");
                        try
                        {
                            if (!Directory.Exists(refAssemblyDirectory2))
                            {
                                Directory.CreateDirectory(refAssemblyDirectory2);
                                refAssemblyDirectory = refAssemblyDirectory2;
                            }
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException($"Could not create ref direcotry {refAssemblyDirectory}", "input", e);
                        }

                    }

                }

                ReferenceAssembly = Path.Combine(refAssemblyDirectory, Path.GetFileName(InputAssembly));
            }
            else
            {
                if (Directory.Exists(outputAssembly))
                {
                    ReferenceAssembly = Path.Combine(outputAssembly, Path.GetFileName(InputAssembly));
                }
                else
                {
                    if (outputAssembly.EndsWith($"{Path.PathSeparator}"))
                    {
                        Directory.CreateDirectory(outputAssembly);
                        ReferenceAssembly = Path.Combine(outputAssembly, Path.GetFileName(InputAssembly));
                    }
                    else
                    {
                        var extension = Path.GetExtension(outputAssembly);
                        if (!extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                        {
                            throw new ArgumentException($"{outputAssembly} must be a DLL", "output");
                        }

                        if (File.Exists(outputAssembly))
                        {
                            ReferenceAssembly = outputAssembly;
                        }
                        else
                        {
                            var refAsmDirectory = Path.GetDirectoryName(outputAssembly);
                            if (!Directory.Exists(refAsmDirectory))
                            {
                                Directory.CreateDirectory(refAsmDirectory);
                            }
                            ReferenceAssembly = outputAssembly;
                        }
                    }
                }
            }
        }

        private void ParseInputAssembly(Options options)
        {
            var inputAssembly = options.InputAssembly;
            if (!File.Exists(inputAssembly))
            {
                throw new ArgumentException($"{inputAssembly} does not exist", nameof(InputAssembly));
            }

            InputAssembly = inputAssembly;
        }

        private void ShowError()
        {
            Parser.ParseArguments<Options>(new string[] { "--help" });
        }

        public Semantics WithParsed(Action<Semantics> options)
        {
            if (Success)
            {
                options?.Invoke(this);
            }

            return this;
        }

        public Semantics WithNotParsed(Action<IEnumerable<ArgumentException>> errors)
        {
            if (!Success)
            {
                errors?.Invoke(ParseExceptions);
            }

            return this;
        }
    }
}
