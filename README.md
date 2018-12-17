# RefGen
## Metadata-only Reference DLL generator for .NET assemblies

### Definition of ref assemblies 

Also see [Roslyn Documentation for /refout](https://github.com/dotnet/roslyn/blob/master/docs/features/refout.md]C:\Users\vatsan\Source\Repos\RefGen\README.md)

Metadata-only assembly have their method bodies replaced with a single `throw null` body, but include all members except anonymous types. The reason for using `throw null` bodies (as opposed to no bodies) is so that `PEVerify` could run and pass (thus validating the completeness of the metadata).

Ref assemblies include an assembly-level `ReferenceAssembly` attribute. This attribute may be specified in source (then we won't need to synthesize it). Because of this attribute, runtimes will refuse to load ref assemblies for execution (but they can still be loaded in reflection-only mode). Some tools may be affected and will need to be updated (for example, `sgen.exe`).

Ref assemblies further remove metadata (private members) from metadata-only assemblies:
 - A ref assembly only has references for what it needs in the API surface. 
   - The real assembly may have additional references related to specific implementations.
   - For instance, the ref assembly for `class C { private void M() { dynamic d = 1; ... } }` does not reference any types required for `dynamic`.
 - Private function-members (methods, properties and events) are removed. 
   - If there are no `InternalsVisibleTo` attributes, do the same for internal function-members.
 - All types (including private or nested types) are kept in ref assemblies. 
 - All attributes are kept (even internal ones).
 - All virtual methods are kept. 
 - Explicit interface implementations are kept. 
 - Explicitly-implemented properties and events are kept, as their accessors are virtual (and are therefore kept).
 - All fields of a struct are kept.

 ### Contributions

 This project is not taking contributions at this time. I will have a basic working version of `RefGen` available in this repo shortly, after which contributions will be taken. 
