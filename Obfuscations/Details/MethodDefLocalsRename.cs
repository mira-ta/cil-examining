using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using CILExamining.Obfuscations.Utilities;

namespace CILExamining.Obfuscations.Details {
    [Obsolete]
    public class MethodDefLocalsRename {
        protected readonly MethodDef methodDef;
        protected readonly RenameUtility renamer;
        protected Random random => renamer.Random;

        public MethodDefLocalsRename(MethodDef methodDef) {
            this.methodDef = methodDef;
            this.renamer = new RenameUtility();
        }

        public MethodDefLocalsRename(MethodDef methodDef, int seed) {
            this.methodDef = methodDef;
            this.renamer = new RenameUtility(seed);
        }

        public MethodDefLocalsRename(MethodDef methodDef, RenameUtility renameUtility) {
            this.methodDef = methodDef;
            this.renamer = renameUtility;
        }

        public void ChangeLocals() {
            LocalList ll = new LocalList(methodDef.Body.Variables.Select(k => new Local(k.Type, renamer.GetObfuscated(false), k.Index)).ToList());
            methodDef.Body = new CilBody(methodDef.Body.InitLocals, methodDef.Body.Instructions, methodDef.Body.ExceptionHandlers, ll);
        }

        public void ChangeLocals(IEnumerable<string> names) {
            IEnumerator<string> en = names.GetEnumerator();

            foreach (Local local in methodDef.Body.Variables) {
                if (!en.MoveNext())
                    throw new ArgumentOutOfRangeException("Not enough names to change method's locals names.");
                local.Name = en.Current;
            }
        }
    }
}
