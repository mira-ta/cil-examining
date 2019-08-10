using dnlib.DotNet;

namespace CILExamining.Obfuscations {
    public interface IObfuscation {
        void Execute(ModuleDefMD moduleDef);
        bool TryExecute(ModuleDefMD moduleDef);
    }
}
