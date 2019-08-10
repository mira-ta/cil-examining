using System;
using System.Collections.Generic;
using System.Linq;


using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace CILExamining
{
    public static class Program {
        public static void Main(string[] args) {
            string path = "../Release/CILExamining.exe";

            ModuleDefMD m = ModuleDefMD.Load(path);
            AssemblyResolver ar = new AssemblyResolver();
            ModuleContext mc = new ModuleContext(ar);
            foreach (AssemblyRef assemblyRef in m.GetAssemblyRefs())
                ar.ResolveThrow(assemblyRef, m);
            ar.PostSearchPaths.Add(path);

            m.Context = mc;

            Obfuscations.Utilities.RenameUtility ru = new Obfuscations.Utilities.RenameUtility();

            new Obfuscations.ReferenceProxyObfuscation(ru.Random).Execute(m);
            //new Obfuscations.RenameObfuscation(ru).Execute(m);
            //new Obfuscations.JunkObfuscation(ru).Execute(m);

            m.Write(path + ".o");
        }
    }
}
