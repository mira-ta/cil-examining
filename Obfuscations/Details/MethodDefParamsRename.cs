using System;
using System.Collections.Generic;

using dnlib.DotNet;

using CILExamining.Obfuscations.Utilities;

namespace CILExamining.Obfuscations.Details {
    public class MethodDefParamsRename {
        protected readonly MethodDef methodDef;
        protected readonly RenameUtility renamer;
        protected Random random => renamer.Random;

        public MethodDefParamsRename(MethodDef methodDef) {
            this.methodDef = methodDef;
            this.renamer = new RenameUtility();
        }
        
        public MethodDefParamsRename(MethodDef methodDef, int seed) {
            this.methodDef = methodDef;
            this.renamer = new RenameUtility(seed);
        }

        public MethodDefParamsRename(MethodDef methodDef, RenameUtility renameUtility) {
            this.methodDef = methodDef;
            this.renamer = renameUtility;
        }

        public void ChangeParams() {
            foreach (Parameter param in methodDef.Parameters) {
                param.CreateParamDef();
                param.Name = renamer.GetObfuscated(false);
            }
        }

        public void ChangeParams(IEnumerable<string> names) {
            IEnumerator<string> en = names.GetEnumerator();

            foreach (Parameter param in methodDef.Parameters) {
                param.CreateParamDef();
                if (!en.MoveNext())
                    throw new ArgumentOutOfRangeException("Not enough names to change method's parameters names.");

                param.Name = en.Current;
            }
        }
    }
}
