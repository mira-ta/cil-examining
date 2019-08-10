# .NET CILExamining

*CILExamining* - is an obfuscator written in C# using `dnlib` library on .NET Framework (.NET Core in the future). This is an experiment for me, owner of the repository, since I was 
bad in RE and some elseshit.

Inspired by `AsStrongAsFuck` obfuscator.

## Running it

Currently it's only `coding based` running, all parameters are being set just in code to test features manually. Soon it'll be refactored.

## Code basis

### Obfuscations

All obfuscations are implementing `IObfuscation` interface, which contains a method `Execute` with one passed parameter - `dnlib.ModuleDef` instance.

To write own obfuscations just make a class implementing `IObfuscation` interface.

```C#

using CILExamining.Obfuscations;

...

public sealed class FooBarObfuscation : IObfuscation {
    ...
    public void Execute(ModuleDef moduleDef) {
        ...
        // Here you can implement logic which will execute obfuscations
        // like executing own methods here.

        // Example:

        this.ObfuscateMethods(moduleDef.Types.Where(k => k.Name.StartsWith("DoObfuscate_")));
        ...
    }
    ...
}

...
```

This class will be found in bootstrap and casted as `IObfuscation` will have been executed.
