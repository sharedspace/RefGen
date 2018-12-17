using System;

namespace RefGen.CommandLine
{
    [Flags]
    public enum AccessModifier
    {
        None        = 0b0000,
        Private     = 0b0001,
        Protected   = 0b0010,
        Internal    = 0b0100,
        Public      = 0b1000, 
    }
}